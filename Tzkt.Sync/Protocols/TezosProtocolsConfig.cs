using Microsoft.Extensions.Configuration;

namespace Tzkt.Sync.Services
{
    public class MavrykProtocolsConfig
    {
        public bool Diagnostics { get; set; } = false;
        public bool Validation { get; set; } = true;
    }

    public static class MavrykProtocolsConfigExt
    {
        public static MavrykProtocolsConfig GetMavrykProtocolsConfig(this IConfiguration config)
        {
            return config.GetSection("Protocols")?.Get<MavrykProtocolsConfig>() ?? new MavrykProtocolsConfig();
        }
    }
}
