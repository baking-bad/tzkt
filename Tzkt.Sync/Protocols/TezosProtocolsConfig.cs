using Microsoft.Extensions.Configuration;

namespace Tzkt.Sync.Services
{
    public class TezosProtocolsConfig
    {
        public bool Diagnostics { get; set; } = false;
        public bool Validation { get; set; } = true;
    }

    public static class TezosProtocolsConfigExt
    {
        public static TezosProtocolsConfig GetTezosProtocolsConfig(this IConfiguration config)
        {
            return config.GetSection("Protocols")?.Get<TezosProtocolsConfig>() ?? new TezosProtocolsConfig();
        }
    }
}
