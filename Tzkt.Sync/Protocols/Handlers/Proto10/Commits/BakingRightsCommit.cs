using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto10
{
    class BakingRightsCommit : Proto3.BakingRightsCommit
    {
        public BakingRightsCommit(ProtocolHandler protocol) : base(protocol) { }

        // Tezos node is no longer able to normally return endorsing rights for a cycle,
        // so we have to temporarily add some crutches, until we implement rights calculation
        protected override async Task<IEnumerable<JsonElement>> GetEndorsingRights(Block block, int cycle)
        {
            Logger.LogInformation("Load endorsing rights");
            try
            {
                Logger.LogInformation("Trying to load by cycle with 30 minutes timeout...");
                #region try aggressive
                using var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
                {
                    BaseAddress = new Uri(Proto.Node.BaseUrl),
                    Timeout = Timeout.InfiniteTimeSpan
                };
                using var cts = new CancellationTokenSource(1800_000);
                using var stream = await client.GetStreamAsync($"chains/main/blocks/{block.Level}/helpers/endorsing_rights?cycle={cycle}", cts.Token);
                using var doc = await JsonDocument.ParseAsync(stream, default, cts.Token);
                var rights = doc.RootElement.Clone().RequiredArray().EnumerateArray();

                if (!rights.Any() || rights.Sum(x => x.RequiredArray("slots").Count()) != block.Protocol.BlocksPerCycle * block.Protocol.EndorsersPerBlock)
                    throw new ValidationException("Rpc returned less endorsing rights (slots) than expected");

                return rights;
                #endregion
            }
            catch
            {
                Logger.LogInformation("Failed to load by cycle. Loading by level for {cnt} blocks with 10 seconds timeout...", block.Protocol.BlocksPerCycle);
                #region throttle
                using var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
                {
                    BaseAddress = new Uri(Proto.Node.BaseUrl),
                    Timeout = Timeout.InfiniteTimeSpan
                };

                var rights = new List<JsonElement>(block.Protocol.BlocksPerCycle * block.Protocol.EndorsersPerBlock / 2);
                var firstLevel = block.Protocol.GetCycleStart(cycle);
                var lastLevel = block.Protocol.GetCycleEnd(cycle);
                var attempts = 0;

                for (int level = firstLevel; level <= lastLevel; level++)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(10_000);
                        using var stream = await client.GetStreamAsync($"chains/main/blocks/{block.Level}/helpers/endorsing_rights?level={level}", cts.Token);
                        using var doc = await JsonDocument.ParseAsync(stream, default, cts.Token);

                        rights.AddRange(doc.RootElement.Clone().RequiredArray().EnumerateArray());
                        attempts = 0;

                        if (level % 128 == 0)
                            Logger.LogInformation("Loaded {cnt} of {total} blocks", level - firstLevel + 1, lastLevel - firstLevel + 1);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to fetch endorsing rights for level {level}. Retrying...", level);
                        if (++attempts >= 30) throw new Exception("Too many RPC errors when fetching endorsing rights");
                        await Task.Delay(1000);
                        level--;
                    }
                }

                if (!rights.Any() || rights.Sum(x => x.RequiredArray("slots").Count()) != block.Protocol.BlocksPerCycle * block.Protocol.EndorsersPerBlock)
                    throw new ValidationException("Rpc returned less endorsing rights (slots) than expected");

                return rights;
                #endregion
            }
        }
    }
}
