using Microsoft.Extensions.Configuration;

namespace Tzkt.Sync.Services
{
    public class TezosNodeConfig
    {
        public string Chain { get; set; }
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
