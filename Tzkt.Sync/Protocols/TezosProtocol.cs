using System;
using Microsoft.Extensions.DependencyInjection;

using Tzkt.Sync.Protocols;

namespace Tzkt.Sync
{
    public static class TezosProtocol
    {
        public static void AddTezosProtocols(this IServiceCollection services)
        {
            services.AddScoped<GenesisHandler>();
            services.AddScoped<InitiatorHandler>();
            services.AddScoped<Proto1Handler>();
            services.AddScoped<Proto2Handler>();
            services.AddScoped<Proto3Handler>();
        }

        public static IProtocolHandler GetProtocolHandler(this IServiceProvider services, string protocol)
        {
            switch (protocol)
            {
                case "":
                case "PrihK96nBAFSxVL1GLJTVhu9YnzkMFiBeuJRPA8NwuZVZCE1L6i":
                    return services.GetRequiredService<GenesisHandler>();
                case "Ps9mPmXaRzmzk35gbAYNCAw6UXdE2qoABTHbN2oEEc1qM7CwT9P":
                    return services.GetRequiredService<InitiatorHandler>();
                case "PtCJ7pwoxe8JasnHY8YonnLYjcVHmhiARPJvqcC6VfHT5s8k8sY":
                    return services.GetRequiredService<Proto1Handler>();
                case "PsYLVpVvgbLhAhoqAkMFUo6gudkJ9weNXhUYCiLDzcUpFpkk8Wt":
                    return services.GetRequiredService<Proto2Handler>();
                case "PsddFKi32cMJ2qPjf43Qv5GDWLDPZb3T3bF6fLKiF5HtvHNU7aP":
                    return services.GetRequiredService<Proto3Handler>();
                default:
                    return services.GetRequiredService<Proto3Handler>();
            }
        }
    }
}
