using Microsoft.Extensions.DependencyInjection;

namespace Tzkt.Sync.Services
{
    public class CacheService
    {
        public AccountManager Accounts { get; }
        public ProtocolManager Protocols { get; }
        public StateManager State { get; }

        public CacheService(AccountManager accounts, ProtocolManager protocols, StateManager state)
        {
            Accounts = accounts;
            Protocols = protocols;
            State = state;
        }
    }

    public static class CacheServiceExt
    {
        public static void AddCaches(this IServiceCollection services)
        {
            services.AddScoped<AccountManager>();
            services.AddScoped<ProtocolManager>();
            services.AddScoped<StateManager>();
            services.AddScoped<CacheService>();
        }
    }
}
