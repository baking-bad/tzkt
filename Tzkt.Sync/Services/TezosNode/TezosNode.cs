using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tzkt.Sync.Services
{
    public sealed class TezosNode : IDisposable
    {
        public RpcClient Rpc { get; private set; }

        readonly string ChainId;
        readonly JsonSerializerOptions DefaultSerializer;

        Header Header;
        Constants Constants;
        DateTime NextBlock;

        public TezosNode(IConfiguration config)
        {
            var nodeConf = config.GetTezosNodeConfig();
            ChainId = nodeConf.ChainId;
            Rpc = new RpcClient(nodeConf.Endpoint, nodeConf.Timeout);

            DefaultSerializer = new JsonSerializerOptions();
            DefaultSerializer.Converters.Add(new JsonInt32Converter());
            DefaultSerializer.Converters.Add(new JsonInt64Converter());
        }

        public Task<Stream> GetBlockAsync(int level)
            => Rpc.GetStreamAsync($"chains/main/blocks/{level}");

        public Task<Stream> GetContractsAsync(int level)
            => Rpc.GetStreamAsync($"chains/main/blocks/{level}/context/raw/json/contracts/index?depth=1");

        public Task<Stream> GetBakingRightsAsync(int level, int cycle, int maxPriority)
            => Rpc.GetStreamAsync($"chains/main/blocks/{level}/helpers/baking_rights?cycle={cycle}&max_priority={maxPriority}");

        public Task<Stream> GetEndorsingRightsAsync(int level, int cycle)
            => Rpc.GetStreamAsync($"chains/main/blocks/{level}/helpers/endorsing_rights?cycle={cycle}");

        public async Task<Header> GetHeaderAsync()
        {
            if (DateTime.UtcNow >= NextBlock)
            {
                var header = await Rpc.GetObjectAsync<Header>(
                    "chains/main/blocks/head/header",
                    DefaultSerializer);

                if (header.ChainId != ChainId)
                    throw new Exception("Invalid chain");

                if (header.Protocol != Header?.Protocol)
                    Constants = await Rpc.GetObjectAsync<Constants>(
                        "chains/main/blocks/head/context/constants",
                        DefaultSerializer);

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
            var header = await Rpc.GetObjectAsync<Header>(
                $"chains/main/blocks/{level}/header",
                DefaultSerializer);

            return header.ChainId == ChainId && header.Hash == hash;
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
