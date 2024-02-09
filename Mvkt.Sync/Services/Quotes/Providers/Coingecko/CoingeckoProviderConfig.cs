﻿using Microsoft.Extensions.Configuration;

namespace Mvkt.Sync.Services
{
    public class CoingeckoProviderConfig
    {
        public string BaseUrl { get; set; } = "https://api.coingecko.com/api/v3/";
        public int Timeout { get; set; } = 30;
    }

    public static class CoingeckoProviderConfigExt
    {
        public static CoingeckoProviderConfig GetCoingeckoProviderConfig(this IConfiguration config)
        {
            return config.GetSection("Quotes:Provider")?.Get<CoingeckoProviderConfig>() ?? new CoingeckoProviderConfig();
        }
    }
}
