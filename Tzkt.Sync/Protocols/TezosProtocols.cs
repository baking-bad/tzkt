using System;
using Microsoft.Extensions.DependencyInjection;

using Tzkt.Sync.Protocols;

namespace Tzkt.Sync
{
    public static class TezosProtocols
    {
        public static void AddTezosProtocols(this IServiceCollection services)
        {
            services.AddScoped<GenesisHandler>();
            services.AddScoped<InitiatorHandler>();
            services.AddScoped<Proto1Handler>();
            services.AddScoped<Proto2Handler>();
            services.AddScoped<Proto3Handler>();
            services.AddScoped<Proto4Handler>();
            services.AddScoped<Proto5Handler>();
            services.AddScoped<Proto6Handler>();
        }

        public static ProtocolHandler GetProtocolHandler(this IServiceProvider services, string protocol)
        {
            switch (protocol)
            {
                case "":
                case "PrihK96nBAFSxVL1GLJTVhu9YnzkMFiBeuJRPA8NwuZVZCE1L6i":
                    return services.GetRequiredService<GenesisHandler>();
                case "Ps9mPmXaRzmzk35gbAYNCAw6UXdE2qoABTHbN2oEEc1qM7CwT9P":
                case "PtBMwNZT94N7gXKw4i273CKcSaBrrBnqnt3RATExNKr9KNX2USV":
                case "PtYuensgYBb3G3x1hLLbCmcav8ue8Kyd2khADcL5LsT5R1hcXex":
                    return services.GetRequiredService<InitiatorHandler>();
                case "PtCJ7pwoxe8JasnHY8YonnLYjcVHmhiARPJvqcC6VfHT5s8k8sY":
                    return services.GetRequiredService<Proto1Handler>();
                case "PsYLVpVvgbLhAhoqAkMFUo6gudkJ9weNXhUYCiLDzcUpFpkk8Wt":
                    return services.GetRequiredService<Proto2Handler>();
                case "PsddFKi32cMJ2qPjf43Qv5GDWLDPZb3T3bF6fLKiF5HtvHNU7aP":
                    return services.GetRequiredService<Proto3Handler>();
                case "Pt24m4xiPbLDhVgVfABUjirbmda3yohdN82Sp9FeuAXJ4eV9otd":
                    return services.GetRequiredService<Proto4Handler>();
                case "PsBabyM1eUXZseaJdmXFApDSBqj8YBfwELoxZHHW77EMcAbbwAS":
                case "PsBABY5HQTSkA4297zNHfsZNKtxULfL18y95qb3m53QJiXGmrbU":
                    return services.GetRequiredService<Proto5Handler>();
                case "PsCARTHAGazKbHtnKfLzQg3kms52kSRpgnDY982a9oYsSXRLQEb":
                    return services.GetRequiredService<Proto6Handler>();
                default:
                    throw new NotImplementedException($"Protocol '{protocol}' is not supported");
            }
        }
    }
}
