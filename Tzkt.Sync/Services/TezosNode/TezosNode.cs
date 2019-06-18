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
        public TezosRpc Rpc { get; }

        private Header Header;
        private DateTime NextBlock;

        public TezosNode(IConfiguration config)
        {
            var nodeConf = config.GetTezosNodeConfig();

            Rpc = new TezosRpc(
                nodeConf.Endpoint,
                nodeConf.Timeout,
                nodeConf.Chain == "test" ? Chain.Test : Chain.Main);
        }
        public void Dispose() => Rpc.Dispose();

        public async Task<JObject> GetBlockAsync(int level)
            => (JObject)await Rpc.Blocks[level].GetAsync();

        public async Task<JArray> GetContractsAsync(int level)
            => (JArray)await Rpc.Blocks[level].Context.Raw.Contracts.GetAsync(1);

        public async Task<JArray> GetBakingRightsAsync(int cycle, int cycleSize, int maxPriority)
            => (JArray)await Rpc.Blocks[cycle * cycleSize + 1].Helpers.BakingRights.GetFromCycleAsync(cycle, maxPriority);

        public async Task<JArray> GetEndorsingRightsAsync(int cycle, int cycleSize)
            => (JArray)await Rpc.Blocks[cycle * cycleSize + 1].Helpers.EndorsingRights.GetFromCycleAsync(cycle);

        public async Task<Header> GetHeaderAsync()
        {
            if (DateTime.UtcNow >= NextBlock)
            {
                Header = await Rpc.Blocks.Head.Header.GetAsync<Header>();
                NextBlock = Header.Timestamp.AddSeconds(60);
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
