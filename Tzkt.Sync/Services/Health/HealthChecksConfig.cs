using Microsoft.Extensions.Configuration;

namespace Tzkt.Sync.Services
{
    class HealthChecksConfig
    {
        public bool Enabled { get; set; } = false;
        public int Delay { get; set; } = 10;
        public int Period { get; set; } = 10;
        public string FilePath { get; set; } = null;
    }

    static class HealthChecksConfigExt
    {
        public static HealthChecksConfig GetHealthChecksConfig(this IConfiguration config)
        {
            return config.GetSection("HealthChecks")?.Get<HealthChecksConfig>() ?? new();
        }
    }
}
