using Microsoft.Extensions.Configuration;

namespace Tzkt.Sync.Services
{
    public class DipDupMetadataConfig
    {
        public bool Enabled { get; set; } = false;
        public string DipDupUrl { get; set; } = "https://metadata.dipdup.net/v1/graphql";
        public string Network { get; set; } = "mainnet";
        public int BatchSize { get; set; } = 100;
        public int PeriodSec { get; set; } = 60;
    }

    public static class DipDupMetadataConfigExt
    {
        public static DipDupMetadataConfig GetDipDupMetadataConfig(this IConfiguration config)
        {
            return config.GetSection("DipDupMetadata")?.Get<DipDupMetadataConfig>() ?? new DipDupMetadataConfig();
        }
    }
}
