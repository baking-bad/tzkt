using Microsoft.Extensions.Configuration;

namespace Mvkt.Sync.Services
{
    public class MavrykExternalDataProviderConfig
    {
        public string BaseUrl { get; set; } = "http://host.docker.internal:3010";
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
