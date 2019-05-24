using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Netezos.Rpc;

namespace Tezzycat.Services
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

        public async Task<Header> GetHeaderAsync()
        {
            if (DateTime.UtcNow > NextBlock)
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
