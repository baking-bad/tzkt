using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Services
{
    public class NodeRpcConfig
    {
        public bool Enabled { get; set; } = true;
        public string Endpoint { get; set; } = "https://rpc.tzkt.io/mainnet";
        public int Timeout { get; set; } = 60;
    }

    public static class TezRpcConfigExt
    {
        public static NodeRpcConfig GetNodeRpcConfig(this IConfiguration config)
        {
            return config.GetSection("NodeRpc")?.Get<NodeRpcConfig>() ?? new NodeRpcConfig();
        }

        public static void ValidateNodeRpcConfig(this IConfiguration config)
        {
            var nodeConfig = config.GetNodeRpcConfig();
            
            if (!nodeConfig.Enabled)
                return;
            
            if (!Uri.TryCreate(nodeConfig.Endpoint, UriKind.Absolute, out _))
            {
                throw new ConfigurationException("Invalid NodeRpc.Endpoint URL");
            }
        }

        public static async Task ValidateNodeRpcChain(this IConfiguration config, StateCache state, NodeRpc rpc)
        {
            var nodeConfig = config.GetNodeRpcConfig();
            if (!nodeConfig.Enabled)
                return;
            
            string chainId;
            try
            {
                chainId = await rpc.GetChainIdAsync();
            }
            catch
            {
                throw new ConfigurationException("Couldn't validate NodeRpc. Provide working Tezos RPC to NodeRpc.Endpoint");
            }
            
            if (chainId != state.Current.ChainId)
            {
                throw new ConfigurationException(
                    $"The API chain ID {chainId} doesn't match the indexer chain ID {state.Current.ChainId}." +
                    $" Please, check that the indexer and API use the same network");
            }
        }
    }
}
