using Microsoft.Extensions.Configuration;

namespace Tzkt.Sync.Services
{
    public class TezosNodeConfig
    {
        public string ChainId { get; set; }
        public string Endpoint { get; set; }
        public int Timeout { get; set; }
    }

    public static class TezosNodeConfigExt
    {
        public static TezosNodeConfig GetTezosNodeConfig(this IConfiguration config)
        {
            return config.GetSection("TezosNode")?.Get<TezosNodeConfig>() ?? new TezosNodeConfig();
        }
    }
}
