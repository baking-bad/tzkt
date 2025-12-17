using Microsoft.Extensions.Configuration;

namespace Mvkt.Sync.Services
{
    public class QuotesServiceConfig
    {
        public bool Async { get; set; } = true;
        public int CopyThreshold { get; set; } = 64;
    }

    public static class QuotesServiceConfigExt
    {
        public static QuotesServiceConfig GetQuotesServiceConfig(this IConfiguration config)
        {
            return config.GetSection("Quotes")?.Get<QuotesServiceConfig>() ?? new QuotesServiceConfig();
        }
    }
}
