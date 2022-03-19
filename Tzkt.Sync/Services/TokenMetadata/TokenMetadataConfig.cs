using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Tzkt.Sync.Services
{
    public class DipDupConfig
    {
        public string Url { get; set; }
        public string MetadataTable { get; set; }
        public string HeadStatusTable { get; set; }
        public string Network { get; set; }
    }

    public class TokenMetadataConfig
    {
        public bool Enabled { get; set; } = false;       
        public int BatchSize { get; set; } = 100;
        public int PeriodSec { get; set; } = 60;
        public List<DipDupConfig> DipDup { get; set; }
        public List<TokenMetadataItem> OverriddenMetadata { get; set; }
    }

    public class TokenMetadataItem
    {
        public string Contract { get; set; }
        public string TokenId { get; set; } = "0";
        public JsonElement Metadata { get; set; }

        public TokenMetadataItem() { }
        public TokenMetadataItem(string contract, string metadata)
        {
            Contract = contract;
            Metadata = JsonSerializer.Deserialize<JsonElement>(metadata);
        }
    }

    public static class TokenMetadataConfigExt
    {
        public static TokenMetadataConfig GetTokenMetadataConfig(this IConfiguration config)
        {
            return config.GetSection("TokenMetadata")?.Get<TokenMetadataConfig>() ?? new TokenMetadataConfig();
        }
    }
}
