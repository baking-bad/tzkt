namespace Tzkt.Sync.Services
{
    public class CacheConfig
    {
        public CacheSize? Accounts { get; set; }
        public CacheSize? BigMapKeys { get; set; }
        public CacheSize? BigMaps { get; set; }
        public CacheSize? Blocks { get; set; }
        public CacheSize? Periods { get; set; }
        public CacheSize? Proposals { get; set; }
        public CacheSize? RefutationGames { get; set; }
        public CacheSize? Schemas { get; set; }
        public CacheSize? SmartRollupCommitments { get; set; }
        public CacheSize? SmartRollupStakes { get; set; }
        public CacheSize? Software { get; set; }
        public CacheSize? StakerCycles { get; set; }
        public CacheSize? Storages { get; set; }
        public CacheSize? TicketBalances { get; set; }
        public CacheSize? Tickets { get; set; }
        public CacheSize? TokenBalances { get; set; }
        public CacheSize? Tokens { get; set; }
        public CacheSize? UnstakeRequests { get; set; }
    }

    public class CacheSize
    {
        public int? SoftCap { get; set; }
        public int? TargetCap { get; set; }
    }

    public static class CacheConfigExt
    {
        public static CacheConfig GetCacheConfig(this IConfiguration config)
        {
            return config.GetSection("Cache")?.Get<CacheConfig>() ?? new();
        }
    }
}
