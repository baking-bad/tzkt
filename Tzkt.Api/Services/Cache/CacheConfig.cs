using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Tzkt.Api.Services
{
    public class CacheConfig
    {
        public double LoadRate = 0.75;
        public int MaxAccounts = 32_000;
    }

    public static class CacheConfigExt
    {
        public static CacheConfig GetCacheConfig(this IConfiguration config)
        {
            return config.GetSection("Cache")?.Get<CacheConfig>() ?? new CacheConfig();
        }
    }
}
