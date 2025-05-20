using System.Reflection;

namespace Tzkt.Api.Services.Cache
{
    public class StateHeadersMiddleware(RequestDelegate next)
    {
        readonly static string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

        public const string TZKT_VERSION = "Tzkt-Version";
        public const string TZKT_LEVEL = "Tzkt-Level";
        public const string TZKT_KNOWN_LEVEL = "Tzkt-Known-Level";
        public const string TZKT_SYNCED_AT = "Tzkt-Synced-At";

        readonly RequestDelegate Next = next;

        public Task InvokeAsync(HttpContext context, StateCache stateCache)
        {
            var state = stateCache.Current;
            context.Response.Headers.Append(TZKT_VERSION, Version);
            context.Response.Headers.Append(TZKT_LEVEL, state.Level.ToString());
            context.Response.Headers.Append(TZKT_KNOWN_LEVEL, state.KnownHead.ToString());
            context.Response.Headers.Append(TZKT_SYNCED_AT, state.LastSync.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            return Next(context);
        }
    }
}
