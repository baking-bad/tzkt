using Microsoft.Extensions.Configuration;

namespace Tzkt.Sync.Services
{
    public class DiagnosticServiceConfig
    {
        public bool Enabled { get; set; }
    }

    public static class DiagnosticServiceConfigExt
    {
        public static DiagnosticServiceConfig GetDiagnosticServiceConfig(this IConfiguration config)
        {
            return config.GetSection("Diagnostics")?.Get<DiagnosticServiceConfig>() ?? new DiagnosticServiceConfig();
        }
    }
}
