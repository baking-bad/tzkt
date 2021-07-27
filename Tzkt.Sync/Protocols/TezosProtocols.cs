﻿using System;
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
            services.AddScoped<Proto7Handler>();
            services.AddScoped<Proto8Handler>();
            services.AddScoped<Proto9Handler>();
            services.AddScoped<Proto10Handler>();
        }

        public static ProtocolHandler GetProtocolHandler(this IServiceProvider services, int level, string protocol)
        {
            if (level > 1)
            {
                return protocol switch
                {
                    "PtCJ7pwoxe8JasnHY8YonnLYjcVHmhiARPJvqcC6VfHT5s8k8sY" => services.GetRequiredService<Proto1Handler>(),
                    "PsYLVpVvgbLhAhoqAkMFUo6gudkJ9weNXhUYCiLDzcUpFpkk8Wt" => services.GetRequiredService<Proto2Handler>(),
                    "PsddFKi32cMJ2qPjf43Qv5GDWLDPZb3T3bF6fLKiF5HtvHNU7aP" => services.GetRequiredService<Proto3Handler>(),
                    "Pt24m4xiPbLDhVgVfABUjirbmda3yohdN82Sp9FeuAXJ4eV9otd" => services.GetRequiredService<Proto4Handler>(),
                    "PsBabyM1eUXZseaJdmXFApDSBqj8YBfwELoxZHHW77EMcAbbwAS" => services.GetRequiredService<Proto5Handler>(),
                    "PsCARTHAGazKbHtnKfLzQg3kms52kSRpgnDY982a9oYsSXRLQEb" => services.GetRequiredService<Proto6Handler>(),
                    "PsDELPH1Kxsxt8f9eWbxQeRxkjfbxoqM52jvs5Y5fBxWWh4ifpo" => services.GetRequiredService<Proto7Handler>(),
                    "PtEdo2ZkT9oKpimTah6x2embF25oss54njMuPzkJTEi5RqfdZFA" => services.GetRequiredService<Proto8Handler>(),
                    "PsFLorenaUUuikDWvMDr6fGBRG8kt3e3D3fHoXK1j1BFRxeSH4i" => services.GetRequiredService<Proto9Handler>(),
                    "PtGRANADsDU8R9daYKAgWnQYAJ64omN1o3KMGVCykShA97vQbvV" => services.GetRequiredService<Proto10Handler>(),
                    _ => throw new NotImplementedException($"Protocol '{protocol}' is not supported"),
                };
            }
            else if (level == 1)
            {
                return services.GetRequiredService<InitiatorHandler>();
            }
            else
            {
                return services.GetRequiredService<GenesisHandler>();
            }
        }
    }
}
