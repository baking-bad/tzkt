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

        public static void ValidateTezRpcConfig(this IConfiguration config, StateCache state)
        {
            var nodeConf = config.GetTezRpcConfig();
            var rpc = new TezosRpc($"{nodeConf.Endpoint}", nodeConf.Timeout);
            var chainId = (rpc.Blocks.Head.Header.GetAsync().Result).chain_id.ToString();
            if (chainId != state.Current.ChainId)
            {
                throw new ConfigurationException($"The API chain ID {chainId.ToString()} doesn't match the indexer chain ID {state.Current.ChainId}." +
                                            $" Please, check that the indexer and API use the same network");
            }
        }
    }
}
