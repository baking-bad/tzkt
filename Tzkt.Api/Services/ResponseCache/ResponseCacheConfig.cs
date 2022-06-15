using Microsoft.Extensions.Configuration;

namespace Tzkt.Api.Services
{
    public class ResponseCacheConfig
    {
        public int CacheSize { get; set; } = 256;
    }

    public static class ResponseCacheConfigExt
    {
        public static ResponseCacheConfig GetOutputCacheConfig(this IConfiguration config)
        {
            return config.GetSection("ResponseCache")?.Get<ResponseCacheConfig>() ?? new();
        }
    }
}
