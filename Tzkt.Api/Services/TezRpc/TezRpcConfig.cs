using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Netezos.Rpc;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Services
{
    public class TezRpcConfig
    {
        public string Endpoint { get; set; } = "https://rpc.tzkt.io/mainnet";
        public int Timeout { get; set; } = 60;
    }

    public static class TezRpcConfigExt
    {
        public static TezRpcConfig GetTezRpcConfig(this IConfiguration config)
        {
            return config.GetSection("TezRpc")?.Get<TezRpcConfig>() ?? new TezRpcConfig();
        }

        public static async Task ValidateTezRpcConfig(this IConfiguration config, StateCache state, NodeRpc rpc)
        {
            var chainId = await rpc.GetChainIdAsync();
            if (chainId != state.Current.ChainId)
            {
                throw new ConfigurationException($"The API chain ID {chainId} doesn't match the indexer chain ID {state.Current.ChainId}." +
                                            $" Please, check that the indexer and API use the same network");
            }
        }
    }
}
