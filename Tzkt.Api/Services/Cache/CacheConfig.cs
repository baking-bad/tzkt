using Microsoft.Extensions.Configuration;

namespace Tzkt.Api.Services
{
    public class CacheConfig
    {
        public double LoadRate { get; set; } = 0.75;
        public int MaxAccounts { get; set; } = 32_000;
    }

    public static class CacheConfigExt
    {
        public static CacheConfig GetCacheConfig(this IConfiguration config)
        {
            return config.GetSection("Cache")?.Get<CacheConfig>() ?? new CacheConfig();
        }
    }
}
