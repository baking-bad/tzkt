using Microsoft.Extensions.DependencyInjection;

namespace Tzkt.Sync.Services
{
    public class CacheService
    {
        public AccountsCache Accounts { get; }
        public ProtocolsCache Protocols { get; }
        public StateCache State { get; }

        public CacheService(AccountsCache accounts, ProtocolsCache protocols, StateCache state)
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
            services.AddScoped<AccountsCache>();
            services.AddScoped<ProtocolsCache>();
            services.AddScoped<StateCache>();
            services.AddScoped<CacheService>();
        }
    }
}
