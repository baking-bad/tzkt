using Microsoft.Extensions.Configuration;

namespace Mvkt.Sync.Services
{
    public class MvktQuotesProviderConfig
    {
        public string BaseUrl { get; set; } = "https://services.mvkt.io";
        public int Timeout { get; set; } = 10;
    }

    public static class MvktQuotesProviderConfigExt
    {
        public static MvktQuotesProviderConfig GetMvktQuotesProviderConfig(this IConfiguration config)
        {
            return config.GetSection("Quotes:Provider")?.Get<MvktQuotesProviderConfig>() ?? new MvktQuotesProviderConfig();
        }
    }
}
