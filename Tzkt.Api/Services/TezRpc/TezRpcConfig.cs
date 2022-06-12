using Microsoft.Extensions.Configuration;

namespace Tzkt.Api.Services
{
    public class TezRpcConfig
    {
        public string Endpoint { get; set; } = "https://rpc.tzkt.io/ithacanet";
        public int Timeout { get; set; } = 60;
    }

    public static class TezRpcConfigExt
    {
        public static TezRpcConfig GetTezRpcConfig(this IConfiguration config)
        {
            return config.GetSection("TezRpc")?.Get<TezRpcConfig>() ?? new TezRpcConfig();
        }
    }
}
