using Mvkt.Sync.Protocols;

namespace Mvkt.Sync
{
    public static class MavrykProtocols
    {
        static string Fallback = null;

        static string GetFallback(IServiceProvider services)
        {
            return Fallback ??= services.GetRequiredService<IConfiguration>().GetValue<string>("Protocols:Fallback");
        }

        public static void AddMavrykProtocols(this IServiceCollection services)
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

        private static ProtocolHandler GetProtocolHandler(IServiceProvider services, string protocol)
        {
            return protocol switch
            {
                "PtAtLasjh71tv2N8SDMtjajR42wTSAd9xFTvXvhDuYfRJPRLSL2" => services.GetRequiredService<Proto18Handler>(),
                _ => null,
            };
        }
    }
}
