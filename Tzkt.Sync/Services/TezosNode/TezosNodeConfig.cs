using Microsoft.Extensions.Configuration;

namespace Tzkt.Sync.Services
{
    public class MavrykNodeConfig
    {
        public string Endpoint { get; set; }
        public int Timeout { get; set; } = 60;
        public int Lag { get; set; } = 0;
    }

    public static class MavrykNodeConfigExt
    {
        public static MavrykNodeConfig GetMavrykNodeConfig(this IConfiguration config)
        {
            return config.GetSection("MavrykNode")?.Get<MavrykNodeConfig>() ?? new MavrykNodeConfig();
        }
    }
}
