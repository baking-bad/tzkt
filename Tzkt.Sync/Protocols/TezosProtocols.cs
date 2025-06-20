﻿using Tzkt.Sync.Protocols;

namespace Tzkt.Sync
{
    public static class TezosProtocols
    {
        static string? Fallback = null;

        static string? GetFallback(IServiceProvider services)
        {
            return Fallback ??= services.GetRequiredService<IConfiguration>().GetValue<string?>("Protocols:Fallback");
        }

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
            services.AddScoped<Proto11Handler>();
            services.AddScoped<Proto12Handler>();
            services.AddScoped<Proto13Handler>();
            services.AddScoped<Proto14Handler>();
            services.AddScoped<Proto15Handler>();
            services.AddScoped<Proto16Handler>();
            services.AddScoped<Proto17Handler>();
            services.AddScoped<Proto18Handler>();
            services.AddScoped<Proto19Handler>();
            services.AddScoped<Proto20Handler>();
            services.AddScoped<Proto21Handler>();
            services.AddScoped<Proto22Handler>();
        }

        public static ProtocolHandler GetProtocolHandler(this IServiceProvider services, int level, string protocol)
        {
            if (level > 1)
            {
                var protocolHandler = GetProtocolHandler(services, protocol);
                if (protocolHandler != null)
                {
                    return protocolHandler;
                }
                var fallback = GetFallback(services);
                if (fallback != null)
                {
                    protocolHandler = GetProtocolHandler(services, fallback);
                    if (protocolHandler != null)
                    {
                        return protocolHandler;
                    }
                }
                throw new NotImplementedException($"Protocol '{protocol}' is not supported");
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

        private static ProtocolHandler? GetProtocolHandler(IServiceProvider services, string protocol)
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
                "PtHangz2aRngywmSRGGvrcTyMbbdpWdpFKuS4uMWxg2RaH9i1qx" => services.GetRequiredService<Proto11Handler>(),
                "Psithaca2MLRFYargivpo7YvUr7wUDqyxrdhC5CQq78mRvimz6A" => services.GetRequiredService<Proto12Handler>(),
                "PtJakart2xVj7pYXJBXrqHgd82rdkLey5ZeeGwDgPp9rhQUbSqY" => services.GetRequiredService<Proto13Handler>(),
                "PtKathmankSpLLDALzWw7CGD2j2MtyveTwboEYokqUCP4a1LxMg" => services.GetRequiredService<Proto14Handler>(),
                "PtLimaPtLMwfNinJi9rCfDPWea8dFgTZ1MeJ9f1m2SRic6ayiwW" => services.GetRequiredService<Proto15Handler>(),
                "PtMumbaiiFFEGbew1rRjzSPyzRbA51Tm3RVZL5suHPxSZYDhCEc" => services.GetRequiredService<Proto16Handler>(),
                "PtMumbai2TmsJHNGRkD8v8YDbtao7BLUC3wjASn1inAKLFCjaH1" => services.GetRequiredService<Proto16Handler>(),
                "PtNairobiyssHuh87hEhfVBGCVrK3WnS8Z2FT4ymB5tAa4r1nQf" => services.GetRequiredService<Proto17Handler>(),
                "ProxfordYmVfjWnRcgjWH36fW6PArwqykTFzotUxRs6gmTcZDuH" => services.GetRequiredService<Proto18Handler>(),
                "PtParisBQscdCm6Cfow6ndeU6wKJyA3aV1j4D3gQBQMsTQyJCrz" => services.GetRequiredService<Proto19Handler>(),
                "PtParisBxoLz5gzMmn3d9WBQNoPSZakgnkMC2VNuQ3KXfUtUQeZ" => services.GetRequiredService<Proto19Handler>(),
                "PsParisCZo7KAh1Z1smVd9ZMZ1HHn5gkzbM94V3PLCpknFWhUAi" => services.GetRequiredService<Proto20Handler>(),
                "PsQuebecnLByd3JwTiGadoG4nGWi3HYiLXUjkibeFV8dCFeVMUg" => services.GetRequiredService<Proto21Handler>(),
                "PsRiotumaAMotcRoDWW1bysEhQy2n1M5fy8JgRp8jjRfHGmfeA7" => services.GetRequiredService<Proto22Handler>(),
                _ => null,
            };
        }
    }
}
