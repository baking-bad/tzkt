using Microsoft.Extensions.Configuration;

namespace Mvkt.Sync.Services
{
    public class MavrykExternalDataProviderConfig
    {
        public string BaseUrl { get; set; }
        public int Timeout { get; set; } = 60;
    }

    public static class MavrykExternalDataProviderConfigExt
    {
        public static MavrykExternalDataProviderConfig GetMavrykExternalDataProviderConfig(this IConfiguration config)
        {
            return config.GetSection("Quotes:Provider")?.Get<MavrykExternalDataProviderConfig>() ?? new MavrykExternalDataProviderConfig();
        }
    }
}
