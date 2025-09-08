namespace Tzkt.Sync.Services
{
    public class TezosNodeConfig
    {
        public string Endpoint { get; set; } = "https://rpc.tzkt.io/mainnet";
        public int Timeout { get; set; } = 60;
    }

    public static class TezosNodeConfigExt
    {
        public static TezosNodeConfig GetTezosNodeConfig(this IConfiguration config)
        {
            return config.GetSection("TezosNode")?.Get<TezosNodeConfig>() ?? new();
        }
    }
}
