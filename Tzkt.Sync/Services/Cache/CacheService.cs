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
        public ScriptsCache Scripts { get; private set; }

        public CacheService(TzktContext db)
        {
            AppState = new AppStateCache(db);
            BakerCycles = new BakerCyclesCache(db);
            BakingRights = new BakingRightsCache(db);
            Accounts = new AccountsCache(this, db);
            Blocks = new BlocksCache(this, db);
            Periods = new PeriodsCache(db);
            Proposals = new ProposalsCache(db);
            Protocols = new ProtocolsCache(db);
            Statistics = new StatisticsCache(db);
            Software = new SoftwareCache(db);
            Scripts = new ScriptsCache(db);
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
            Scripts.Reset();

            await AppState.ResetAsync();
            await Accounts.ResetAsync();
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
