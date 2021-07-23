using Microsoft.Extensions.Configuration;

namespace Tzkt.Api.Services
{
    class HealthChecksConfig
    {
        public bool Enabled { get; set; } = false;
        public string Endpoint { get; set; } = "/health";
    }

    static class HealthChecksConfigExt
    {
        public static HealthChecksConfig GetHealthChecksConfig(this IConfiguration config)
        {
            return config.GetSection("HealthChecks")?.Get<HealthChecksConfig>() ?? new();
        }
    }
}
