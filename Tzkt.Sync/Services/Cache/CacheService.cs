using Microsoft.Extensions.DependencyInjection;

namespace Tzkt.Sync.Services
{
    public static class CacheService
    {
        public static void AddCaches(this IServiceCollection services)
        {
            services.AddScoped<AccountsCache>();
            services.AddScoped<ProtocolsCache>();
            services.AddScoped<StateCache>();
        }
    }
}
