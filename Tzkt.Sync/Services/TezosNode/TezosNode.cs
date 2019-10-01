using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

using Netezos.Rpc;

namespace Tzkt.Sync.Services
{
    public class TezosNode : IDisposable
    {
        public TezosRpc Rpc { get; private set; }

        private string ChainId;
        private Constants Constants;
        private Header Header;
        private DateTime NextBlock;

        public TezosNode(IConfiguration config)
        {
            var nodeConf = config.GetTezosNodeConfig();
            ChainId = nodeConf.ChainId;
            Rpc = new TezosRpc(nodeConf.Endpoint, nodeConf.Timeout);
        }
        public void Dispose() => Rpc.Dispose();

        public async Task<JObject> GetBlockAsync(int level)
            //=> (JObject)await Rpc.Blocks[level].GetAsync();
            => (JObject)await Rpc.GetAsync($"chains/main/blocks/{level}");

        public async Task<JArray> GetContractsAsync(int level)
            => (JArray)await Rpc.Blocks[level].Context.Raw.Contracts.GetAsync(depth: 1);

        public async Task<JArray> GetBakingRightsAsync(int level, int cycle, int maxPriority)
            => (JArray)await Rpc.Blocks[level].Helpers.BakingRights.GetFromCycleAsync(cycle, maxPriority);

        public async Task<JArray> GetEndorsingRightsAsync(int level, int cycle)
            => (JArray)await Rpc.Blocks[level].Helpers.EndorsingRights.GetFromCycleAsync(cycle);

        public async Task<Header> GetHeaderAsync()
        {
            if (DateTime.UtcNow >= NextBlock)
            {
                var header = await Rpc.Blocks.Head.Header.GetAsync<Header>();

                if (header.ChainId != ChainId)
                    throw new Exception("Invalid chain");

                if (header.Protocol != Header?.Protocol)
                    Constants = await Rpc.Blocks.Head.Context.Constants.GetAsync<Constants>();

                NextBlock = header.Timestamp.AddSeconds(
                    header.Level != Header?.Level
                    ? Constants.BlockIntervals[0]
                    : 1);

                Header = header;
            }

            return Header;
        }

        public async Task<bool> HasUpdatesAsync(int level)
        {
            var header = await GetHeaderAsync();
            return header.Level != level;
        }

        public async Task<bool> ValidateBranchAsync(int level, string hash)
        {
            var header = await Rpc.Blocks[level].Header.GetAsync<Header>();
            return header.Hash == hash;
        }
    }

    public static class TezosNodeExt
    {
        public static void AddTezosNode(this IServiceCollection services)
        {
            services.AddSingleton<TezosNode>();
        }
    }
}
