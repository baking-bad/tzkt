namespace Tzkt.Sync.Services
{
    public class EvmNodeConfig
    {
        public required string Endpoint { get; set; }
        public int Timeout { get; set; } = 10;
    }

    public static class EvmNodeConfigExt
    {
        public static EvmNodeConfig GetEvmNodeConfig(this IConfiguration config)
        {
            return config.GetSection("EvmNode")?.Get<EvmNodeConfig>()
                ?? throw new Exception("EvmNode is not configured");
        }
    }
}
