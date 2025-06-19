using Tzkt.Data;
using Tzkt.Sync.Services.Cache;

namespace Tzkt.Sync.Services
{
    public class CacheService
    {
        public AccountsCache Accounts { get; }
        public AppStateCache AppState { get; }
        public BakerCyclesCache BakerCycles { get; }
        public BakingRightsCache BakingRights { get; }
        public BigMapKeysCache BigMapKeys { get; }
        public BigMapsCache BigMaps { get; }
        public BlocksCache Blocks { get; }
        public PeriodsCache Periods { get; }
        public ProposalsCache Proposals { get; }
        public ProtocolsCache Protocols { get; }
        public RefutationGameCache RefutationGames { get; }
        public SchemasCache Schemas { get; }
        public SmartRollupCommitmentCache SmartRollupCommitments { get; }
        public SmartRollupStakesCache SmartRollupStakes { get; }
        public SoftwareCache Software { get; }
        public StakerCyclesCache StakerCycles { get; }
        public StatisticsCache Statistics { get; }
        public StoragesCache Storages { get; }
        public TicketBalancesCache TicketBalances { get; }
        public TicketsCache Tickets { get; }
        public TokenBalancesCache TokenBalances { get; }
        public TokensCache Tokens { get; }
        public UnstakeRequestsCache UnstakeRequests { get; }

        public CacheService(TzktContext db)
        {
            Accounts = new(this, db);
            AppState = new(db);
            BakerCycles = new(db);
            BakingRights = new(db);
            BigMapKeys = new(db);
            BigMaps = new(db);
            Blocks = new(this, db);
            Periods = new(db);
            Proposals = new(db);
            Protocols = new(db);
            RefutationGames = new(db);
            Schemas = new(db);
            SmartRollupCommitments = new(db);
            SmartRollupStakes = new(db);
            Software = new(db);
            StakerCycles = new(db);
            Statistics = new(db);
            Storages = new(db);
            TicketBalances = new(db);
            Tickets = new(db);
            TokenBalances = new(db);
            Tokens = new(db);
            UnstakeRequests = new(db);
        }

        public async Task ResetAsync()
        {
            await Accounts.ResetAsync();
            await AppState.ResetAsync();
            BakerCycles.Reset();
            BakingRights.Reset();
            BigMapKeys.Reset();
            BigMaps.Reset();
            Blocks.Reset();
            Periods.Reset();
            Proposals.Reset();
            await Protocols.ResetAsync();
            RefutationGames.Reset();
            Schemas.Reset();
            SmartRollupCommitments.Reset();
            SmartRollupStakes.Reset();
            Software.Reset();
            StakerCycles.Reset();
            await Statistics.ResetAsync();
            Storages.Reset();
            TicketBalances.Reset();
            Tickets.Reset();
            TokenBalances.Reset();
            Tokens.Reset();
            UnstakeRequests.Reset();
        }

        public void Trim()
        {
            Accounts.Trim();
            BigMapKeys.Trim();
            BigMaps.Trim();
            Blocks.Trim();
            Periods.Trim();
            Proposals.Trim();
            RefutationGames.Trim();
            Schemas.Trim();
            SmartRollupCommitments.Trim();
            SmartRollupStakes.Trim();
            Software.Trim();
            StakerCycles.Trim();
            Storages.Trim();
            TicketBalances.Trim();
            Tickets.Trim();
            TokenBalances.Trim();
            Tokens.Trim();
            UnstakeRequests.Trim();
        }
    }

    public static class CacheServiceExt
    {
        public static void AddCache(this IServiceCollection services, IConfiguration config)
        {
            var cacheConfig = config.GetCacheConfig();
            AccountsCache.Configure(cacheConfig.Accounts);
            BigMapKeysCache.Configure(cacheConfig.BigMapKeys);
            BigMapsCache.Configure(cacheConfig.BigMaps);
            BlocksCache.Configure(cacheConfig.Blocks);
            PeriodsCache.Configure(cacheConfig.Periods);
            ProposalsCache.Configure(cacheConfig.Proposals);
            RefutationGameCache.Configure(cacheConfig.RefutationGames);
            SchemasCache.Configure(cacheConfig.Schemas);
            SmartRollupCommitmentCache.Configure(cacheConfig.SmartRollupCommitments);
            SmartRollupStakesCache.Configure(cacheConfig.SmartRollupStakes);
            SoftwareCache.Configure(cacheConfig.Software);
            StakerCyclesCache.Configure(cacheConfig.StakerCycles);
            StoragesCache.Configure(cacheConfig.Storages);
            TicketBalancesCache.Configure(cacheConfig.TicketBalances);
            TicketsCache.Configure(cacheConfig.Tickets);
            TokenBalancesCache.Configure(cacheConfig.TokenBalances);
            TokensCache.Configure(cacheConfig.Tokens);
            UnstakeRequestsCache.Configure(cacheConfig.UnstakeRequests);

            services.AddScoped<CacheService>();
        }
    }
}
