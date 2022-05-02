using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Tzkt.Api.Services.Cache
{
    public class StateHeadersMiddleware
    {
        readonly static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public const string TZKT_VERSION = "Tzkt-Version";
        public const string TZKT_LEVEL = "Tzkt-Level";
        public const string TZKT_KNOWN_LEVEL = "Tzkt-Known-Level";
        public const string TZKT_SYNCED_AT = "Tzkt-Synced-At";

        readonly RequestDelegate Next;
        public StateHeadersMiddleware(RequestDelegate next) => Next = next;

        public Task InvokeAsync(HttpContext context, StateCache stateCache)
        {
            var state = stateCache.Current;
            context.Response.Headers.Add(TZKT_VERSION, Version);
            context.Response.Headers.Add(TZKT_LEVEL, state.Level.ToString());
            context.Response.Headers.Add(TZKT_KNOWN_LEVEL, state.KnownHead.ToString());
            context.Response.Headers.Add(TZKT_SYNCED_AT, state.LastSync.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            return Next(context);
        }
    }
}
