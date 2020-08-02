using Microsoft.Extensions.Configuration;

namespace Tzkt.Sync.Services
{
    public class TzktQuotesProviderConfig
    {
        public string BaseUrl { get; set; } = "https://services.tzkt.io";
        public int Timeout { get; set; } = 10;
    }

    public static class TzktQuotesProviderConfigExt
    {
        public static TzktQuotesProviderConfig GetTzktQuotesProviderConfig(this IConfiguration config)
        {
            return config.GetSection("Quotes:Provider")?.Get<TzktQuotesProviderConfig>() ?? new TzktQuotesProviderConfig();
        }
    }
}
