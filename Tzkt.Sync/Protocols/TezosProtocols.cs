using Tzkt.Data.Models;
using Tzkt.Sync.Protocols;

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
            services.AddScoped<Proto24Handler>();
        }

        public static ProtocolHandler GetNextBlockHandler(this IServiceProvider services, AppState state)
        {
            return GetProtocolOrFallbackHandler(services, state.NextProtocol);
        }

        public static ProtocolHandler GetCurrentBlockHandler(this IServiceProvider services, AppState state)
        {
            return GetProtocolOrFallbackHandler(services, state.Protocol);
        }

        private static ProtocolHandler GetProtocolOrFallbackHandler(IServiceProvider services, string protocol)
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

        private static ProtocolHandler? GetProtocolHandler(IServiceProvider services, string protocol)
        {
            return protocol switch
            {
                "PrihK96nBAFSxVL1GLJTVhu9YnzkMFiBeuJRPA8NwuZVZCE1L6i" => services.GetRequiredService<Proto24Handler>(),
                "Ps9mPmXaRzmzk35gbAYNCAw6UXdE2qoABTHbN2oEEc1qM7CwT9P" => services.GetRequiredService<Proto24Handler>(),
                "PtTALLiNtPec7mE7yY4m3k26J8Qukef3E3ehzhfXgFZKGtDdAXu" => services.GetRequiredService<Proto24Handler>(),
                _ => null,
            };
        }
    }
}
