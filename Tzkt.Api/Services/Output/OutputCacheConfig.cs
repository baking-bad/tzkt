using Microsoft.Extensions.Configuration;

namespace Tzkt.Api.Services
{
    public class OutputCacheConfig
    {
        public int CacheSize { get; set; } = 500_000_000;
    }

    public static class OutputCacheConfigExt
    {
        public static OutputCacheConfig GetOutputCacheConfig(this IConfiguration config)
        {
            return config.GetSection("OutputCache")?.Get<OutputCacheConfig>() ?? new OutputCacheConfig();
        }
    }
}
