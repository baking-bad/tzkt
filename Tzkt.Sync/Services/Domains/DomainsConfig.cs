using Microsoft.Extensions.Configuration;

namespace Tzkt.Sync.Services.Domains
{
    public class DomainsConfig
    {
        public bool Enabled { get; set; } = false;
        public string NameRegistry { get; set; } = "KT1GBZmSxmnKJXGMdMLbugPfLyUPmuLSMwKS";
        public int PeriodSec { get; set; } = 30;
    }

    public static class DomainsConfigExt
    {
        public static DomainsConfig GetDomainsConfig(this IConfiguration config)
        {
            return config.GetSection("Domains")?.Get<DomainsConfig>() ?? new();
        }
    }
}
