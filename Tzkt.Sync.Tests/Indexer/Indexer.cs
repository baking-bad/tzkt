using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Tests
{
    internal class Indexer
    {
        public static async Task RunAsync(IHost app, CancellationToken ct)
        {
            var logger = app.Services.GetRequiredService<ILogger<Indexer>>();

            var state = await GetAppStateAsync(app);
            logger.LogInformation("Indexer head: {level}", state.Level);

            var head = await GetNodeHeadAsync(app);
            logger.LogInformation("Node head: {level}", head.Level);

            while (state.Level < head.Level && !ct.IsCancellationRequested)
            {
                logger.LogInformation("Applying {level} of {total}", state.Level + 1, head.Level);

                using var scope = app.Services.CreateScope();
                var protocol = scope.ServiceProvider.GetProtocolHandler(state.Level + 1, state.NextProtocol);
                state = await protocol.CommitBlock(head.Level);
            }

            logger.LogInformation("Indexer is synced");
        }

        static async Task<AppState> GetAppStateAsync(IHost host)
        {
            using var scope = host.Services.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<CacheService>();
            await cache.ResetAsync();
            return cache.AppState.Get();
        }

        static async Task<Header> GetNodeHeadAsync(IHost host)
        {
            using var scope = host.Services.CreateScope();
            var node = scope.ServiceProvider.GetRequiredService<TezosNode>();
            return await node.GetHeaderAsync();
        }
    }
}
