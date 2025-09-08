using System.Text.Json;

namespace Tzkt.Sync.Services
{
    public class TokenMetadataConfig
    {
        public bool Enabled { get; set; } = false;       
        public int BatchSize { get; set; } = 100;
        public int PeriodSec { get; set; } = 60;
        public List<DipDupConfig> DipDup { get; set; } = [];
        public List<TokenMetadataItem> OverriddenMetadata { get; set; } = [];
    }

    public class DipDupConfig
    {
        public string Url { get; set; } = "https://metadata.dipdup.net/v1/graphql";
        public int Timeout { get; set; } = 60;
        public string MetadataTable { get; set; } = "dipdup_token_metadata";
        public string HeadStatusTable { get; set; } = "dipdup_head";
        public string Network { get; set; } = "mainnet";
        public int SelectLimit { get; set; } = 10_000;
        public DipDupFilter? Filter { get; set; }
    }

    public class DipDupFilter
    {
        public FilterMode Mode { get; set; } = FilterMode.Exclude;
        public HashSet<string> Contracts { get; set; } = [];

        public enum FilterMode
        {
            Exclude,
            Include
        }
    }

    public class TokenMetadataItem
    {
        public required string Contract { get; set; }
        public string TokenId { get; set; } = "0";
        public JsonElement? Metadata { get; set; }

        public TokenMetadataItem() {}
        public TokenMetadataItem(string contract, string? metadata)
        {
            Contract = contract;
            Metadata = metadata is string json
                ? JsonSerializer.Deserialize<JsonElement>(json)
                : null;
        }
    }

    public static class TokenMetadataConfigExt
    {
        public static TokenMetadataConfig GetTokenMetadataConfig(this IConfiguration config)
        {
            return config.GetSection("TokenMetadata")?.Get<TokenMetadataConfig>() ?? new();
        }
    }
}
