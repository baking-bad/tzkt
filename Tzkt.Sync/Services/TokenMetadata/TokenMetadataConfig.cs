using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Tzkt.Sync.Services
{
    public class TokenMetadataConfig
    {
        public bool Enabled { get; set; } = false;
        public string DipDupUrl { get; set; } = "https://metadata.dipdup.net/v1/graphql";
        public string Network { get; set; } = "mainnet";
        public int BatchSize { get; set; } = 100;
        public int PeriodSec { get; set; } = 60;

        public List<TokenMetadataItem> OverriddenMetadata { get; set; } = new()
        {
            new("KT1PWx2mnDueood7fEmfbBDKx1D9BAnnXitn", @"{""name"":""tzBTC"",""symbol"":""tzBTC"",""decimals"":""8""}"),
            new("KT1VYsVfmobT7rsMVivvZ4J8i3bPiqz12NaH", @"{""name"":""wXTZ"",""symbol"":""wXTZ"",""decimals"":""6""}"),
            new("KT1LN4LPSqTMS7Sd2CJw4bbDGRkMv2t68Fy9", @"{""name"":""USDtez"",""symbol"":""USDtz"",""decimals"":""6""}"),
            new("KT19at7rQUvyjxnZ2fBv7D9zc8rkyG7gAoU8", @"{""name"":""ETHtez"",""symbol"":""ETHtz"",""decimals"":""18""}"),
            new("KT1REEb5VxWRjcHm5GzDMwErMmNFftsE5Gpf", @"{""name"":""Stably USD"",""symbol"":""USDS"",""decimals"":""6""}"),
            new("KT1AEfeckNbdEYwaMKkytBwPJPycz7jdSGea", @"{""name"":""STKR"",""symbol"":""STKR"",""decimals"":""18""}"),
            new("KT1AafHA1C1vk959wvHWBispY9Y2f3fxBUUo", @"{""name"":""LB Token"",""symbol"":""LBT"",""decimals"":""0""}"),
            new("KT1K9gCRgaLRFKTErYt1wVxA3Frb9FjasjTV", @"{""name"":""Kolibri USD"",""symbol"":""kUSD"",""decimals"":""18""}"),
            new("KT1AxaBxkFLCUi3f8rdDAAxBKHfzY8LfKDRA", @"{""name"":""Quipuswap Liquidating kUSD"",""symbol"":""QLkUSD"",""decimals"":""36""}"),
            new("KT1AFA2mwNUMNd4SsujE1YYp29vd8BZejyKW", @"{""name"":""Hic et nunc DAO"",""symbol"":""hDAO"",""decimals"":""6""}"),
            new("KT1LqEyTQxD2Dsdkk4LME5YGcBqazAwXrg4t", @"{""name"":""Werecoin"",""symbol"":""WRC"",""decimals"":""6""}")
        };
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
