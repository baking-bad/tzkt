using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto10
{
    class BakingRightsCommit(ProtocolHandler protocol) : Proto3.BakingRightsCommit(protocol)
    {
        // Tezos node is no longer able to normally return attestation rights for a cycle,
        // so we have to temporarily add some crutches, until we implement rights calculation
        protected override async Task<IEnumerable<JsonElement>> GetAttestationRights(Block block, Protocol protocol, int cycle)
        {
            Logger.LogInformation("Load attestation rights");
            try
            {
                Logger.LogInformation("Trying to load by cycle with 30 minutes timeout...");
                #region try aggressive
                var res = await Proto.Node.GetAsync($"chains/main/blocks/{block.Level}/helpers/endorsing_rights?cycle={cycle}", TimeSpan.FromMinutes(30));
                
                var rights = res.RequiredArray().EnumerateArray();
                if (!rights.Any() || rights.Sum(x => x.RequiredArray("slots").Count()) != protocol.BlocksPerCycle * protocol.AttestersPerBlock)
                    throw new ValidationException("Rpc returned less attestation rights (slots) than expected");

                return rights;
                #endregion
            }
            catch
            {
                Logger.LogInformation("Failed to load by cycle. Loading by level for {cnt} blocks with 10 seconds timeout...", protocol.BlocksPerCycle);
                #region throttle
                var rights = new List<JsonElement>(protocol.BlocksPerCycle * protocol.AttestersPerBlock / 2);
                var firstLevel = protocol.GetCycleStart(cycle);
                var lastLevel = protocol.GetCycleEnd(cycle);
                var attempts = 0;

                for (int level = firstLevel; level <= lastLevel; level++)
                {
                    try
                    {
                        var res = await Proto.Node.GetAsync($"chains/main/blocks/{block.Level}/helpers/endorsing_rights?level={level}", TimeSpan.FromSeconds(10));

                        rights.AddRange(res.RequiredArray().EnumerateArray());
                        attempts = 0;

                        if (level % 128 == 0)
                            Logger.LogInformation("Loaded {cnt} of {total} blocks", level - firstLevel + 1, lastLevel - firstLevel + 1);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to fetch attestation rights for level {level}. Retrying...", level);
                        if (++attempts >= 30) throw new Exception("Too many RPC errors when fetching attestation rights");
                        await Task.Delay(1000);
                        level--;
                    }
                }

                if (rights.Count == 0 || rights.Sum(x => x.RequiredArray("slots").Count()) != protocol.BlocksPerCycle * protocol.AttestersPerBlock)
                    throw new ValidationException("Rpc returned less attestation rights (slots) than expected");

                return rights;
                #endregion
            }
        }
    }
}
