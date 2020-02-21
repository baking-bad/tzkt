using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tzkt.Sync.Services
{
    public sealed class TezosNode : IDisposable
    {
        readonly string ChainId;
        readonly RpcClient Rpc;

        Header Header;
        Constants Constants;
        DateTime NextBlock;

        public TezosNode(IConfiguration config)
        {
            var nodeConf = config.GetTezosNodeConfig();
            ChainId = nodeConf.ChainId;
            Rpc = new RpcClient(nodeConf.Endpoint, nodeConf.Timeout);
        }

        public Task<Stream> GetBlockAsync(int level)
            => Rpc.GetStreamAsync($"chains/main/blocks/{level}");

        public Task<Stream> GetConstantsAsync(int level)
            => Rpc.GetStreamAsync($"chains/main/blocks/{level}/context/constants");

        public Task<Stream> GetContractsAsync(int level)
            => Rpc.GetStreamAsync($"chains/main/blocks/{level}/context/raw/json/contracts/index?depth=1");

        public Task<Stream> GetGlobalCounterAsync(int level)
            => Rpc.GetStreamAsync($"chains/main/blocks/{level}/context/raw/json/contracts/global_counter");

        public Task<Stream> GetContractAsync(int level, string address)
            => Rpc.GetStreamAsync($"chains/main/blocks/{level}/context/contracts/{address}");

        public Task<Stream> GetDelegateAsync(int level, string address)
            => Rpc.GetStreamAsync($"chains/main/blocks/{level}/context/delegates/{address}");

        public Task<Stream> GetBakingRightsAsync(int level, int cycle, int maxPriority)
            => Rpc.GetStreamAsync($"chains/main/blocks/{level}/helpers/baking_rights?cycle={cycle}&max_priority={maxPriority}");

        public Task<Stream> GetEndorsingRightsAsync(int level, int cycle)
            => Rpc.GetStreamAsync($"chains/main/blocks/{level}/helpers/endorsing_rights?cycle={cycle}");

        public async Task<Header> GetHeaderAsync()
        {
            if (DateTime.UtcNow >= NextBlock)
            {
                var header = await Rpc.GetObjectAsync<Header>("chains/main/blocks/head/header");

                if (header.ChainId != ChainId)
                    throw new Exception("Invalid chain");

                if (header.Protocol != Header?.Protocol)
                    Constants = await Rpc.GetObjectAsync<Constants>("chains/main/blocks/head/context/constants");

                NextBlock = header.Level != Header?.Level
                    ? header.Timestamp.AddSeconds(Constants.BlockIntervals[0])
                    : DateTime.UtcNow.AddSeconds(1);

                Header = header;
            }

            return Header;
        }

        public async Task<Header> GetHeaderAsync(int level)
        {
            var header = await Rpc.GetObjectAsync<Header>($"chains/main/blocks/{level}/header");

            if (header.ChainId != ChainId)
                throw new Exception("Invalid chain");

            return header;
        }

        public async Task<bool> HasUpdatesAsync(int level)
        {
            var header = await GetHeaderAsync();
            return header.Level != level;
        }

        public void Dispose() => Rpc.Dispose();
    }

    public static class TezosNodeExt
    {
        public static void AddTezosNode(this IServiceCollection services)
        {
            services.AddSingleton<TezosNode>();
        }
    }
}
