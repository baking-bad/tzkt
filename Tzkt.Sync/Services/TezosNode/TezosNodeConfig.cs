using Microsoft.Extensions.Configuration;

namespace Tzkt.Sync.Services
{
    public class TezosNodeConfig
    {
        public string Endpoint { get; set; }
        public int Timeout { get; set; } = 60;
        public int Lag { get; set; } = 0;
    }

    public static class TezosNodeConfigExt
    {
        public static TezosNodeConfig GetTezosNodeConfig(this IConfiguration config)
        {
            return config.GetSection("TezosNode")?.Get<TezosNodeConfig>() ?? new TezosNodeConfig();
        }
    }
}
