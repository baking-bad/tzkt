namespace Tzkt.Sync.Services
{
    public class QuotesServiceConfig
    {
        public bool Async { get; set; } = true;
    }

    public static class QuotesServiceConfigExt
    {
        public static QuotesServiceConfig GetQuotesServiceConfig(this IConfiguration config)
        {
            return config.GetSection("Quotes")?.Get<QuotesServiceConfig>() ?? new();
        }
    }
}
