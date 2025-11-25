using Microsoft.Extensions.Configuration;

namespace Mvkt.Sync.Services
{
    public class MavrykExternalDataProviderConfig
    {
        public string BaseUrl { get; set; } = "https://services.api.mavryk.network";
        public int Timeout { get; set; } = 10;
    }

    public static class MavrykExternalDataProviderConfigExt
    {
        public static MavrykExternalDataProviderConfig GetMavrykExternalDataProviderConfig(this IConfiguration config)
        {
            return config.GetSection("Quotes:Provider")?.Get<MavrykExternalDataProviderConfig>() ?? new MavrykExternalDataProviderConfig();
        }
    }
}
