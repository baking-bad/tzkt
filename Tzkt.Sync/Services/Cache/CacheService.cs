using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

using Tzkt.Data;
using Tzkt.Sync.Services.Cache;

namespace Tzkt.Sync.Services
{
    public class CacheService
    {
        public AppStateCache AppState { get; private set; }
        public AccountsCache Accounts { get; private set; }
        public BakerCyclesCache BakerCycles { get; private set; }
        public BakingRightsCache BakingRights { get; private set; }
        public BlocksCache Blocks { get; private set; }
        public PeriodsCache Periods { get; private set; }
        public ProposalsCache Proposals { get; private set; }
        public ProtocolsCache Protocols { get; private set; }
        public StatisticsCache Statistics { get; private set; }
        public SoftwareCache Software { get; private set; }
        public SchemasCache Schemas { get; private set; }
        public StoragesCache Storages { get; private set; }
        public BigMapsCache BigMaps { get; private set; }
        public BigMapKeysCache BigMapKeys { get; private set; }
        public TokensCache Tokens { get; private set; }
        public TokenBalancesCache TokenBalances { get; private set; }

        public CacheService(TzktContext db)
        {
            AppState = new(db);
            BakerCycles = new(db);
            BakingRights = new(db);
            Accounts = new(this, db);
            Blocks = new(this, db);
            Periods = new(db);
            Proposals = new(db);
            Protocols = new(db);
            Statistics = new(db);
            Software = new(db);
            Schemas = new(db);
            Storages = new(db);
            BigMaps = new(db);
            BigMapKeys = new(db);
            Tokens = new(db);
            TokenBalances = new(db);
        }

        public async Task ResetAsync()
        {
            BakerCycles.Reset();
            BakingRights.Reset();
            Blocks.Reset();
            Protocols.Reset();
            Proposals.Reset();
            Periods.Reset();
            Statistics.Reset();
            Software.Reset();
            Schemas.Reset();
            Storages.Reset();
            BigMaps.Reset();
            BigMapKeys.Reset();
            Tokens.Reset();
            TokenBalances.Reset();

            await AppState.ResetAsync();
            await Accounts.ResetAsync();
        }

        public void Trim()
        {
            Tokens.Trim();
            TokenBalances.Trim();
        }
    }

    public static class CacheServiceExt
    {
        public static void AddCaches(this IServiceCollection services)
        {
            services.AddScoped<CacheService>();
        }
    }
}
