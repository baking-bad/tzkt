using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services
{
    public sealed class TezosNode : IDisposable
    {
        public string BaseUrl { get; }
        readonly TezosNodeConfig Config;
        readonly TzktClient Rpc;
        readonly IServiceScopeFactory Services;
        readonly ILogger Logger;

        Header Header;
        Constants Constants;
        DateTime NextBlock;
        DateTime NextSyncStateUpdate;

        public TezosNode(IServiceScopeFactory services, IConfiguration config, ILogger<TezosNode> logger)
        {
            Config = config.GetTezosNodeConfig();
            BaseUrl = $"{Config.Endpoint.TrimEnd('/')}/";
            Rpc = new TzktClient(BaseUrl, Config.Timeout);
            Services = services;
            Logger = logger;
        }

        public async Task<JsonElement> GetAsync(string url)
        {
            using var stream = await Rpc.GetStreamAsync(url);
            using var doc = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { MaxDepth = 100_000 });
            return doc.RootElement.Clone();
        }

        public async Task<Header> GetHeaderAsync()
        {
            if (DateTime.UtcNow >= NextBlock)
            {
                var header = await Rpc.GetObjectAsync<Header>(Config.Lag > 0
                    ? $"chains/main/blocks/head~{Config.Lag}/header"
                    : "chains/main/blocks/head/header");

                if (header.Protocol != Header?.Protocol)
                    Constants = await Rpc.GetObjectAsync<Constants>($"chains/main/blocks/{header.Level}/context/constants");

                NextBlock = header.Level != Header?.Level
                    ? header.Timestamp.AddSeconds((Constants.MinBlockDelay ?? Constants.BlockIntervals[0]) * (Config.Lag + 1))
                    : DateTime.UtcNow.AddSeconds(1);

                #region update last sync
                if (header.Level != Header?.Level || DateTime.UtcNow >= NextSyncStateUpdate)
                {
                    try
                    {
                        using var scope = Services.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<TzktContext>();
                        var cache = scope.ServiceProvider.GetRequiredService<CacheService>();

                        var syncTime = DateTime.UtcNow;
                        await db.Database.ExecuteSqlRawAsync($@"
                        UPDATE  ""{nameof(TzktContext.AppState)}""
                        SET     ""{nameof(AppState.KnownHead)}"" = {header.Level},
                                ""{nameof(AppState.LastSync)}"" = '{syncTime:yyyy-MM-ddTHH:mm:ssZ}';");
                        cache.AppState.UpdateSyncState(header.Level, syncTime);

                        NextSyncStateUpdate = syncTime.AddSeconds(5);
                    }
                    catch (Exception ex)
                    {
                        // no big deal...
                        Logger.LogWarning(ex, "Failed to update AppState");
                    }
                }
                #endregion

                Header = header;
            }

            return Header;
        }

        public Task<Header> GetHeaderAsync(int level)
        {
            return Rpc.GetObjectAsync<Header>($"chains/main/blocks/{level}/header");
        }

        public async Task<bool> HasUpdatesAsync(int level)
        {
            var header = await GetHeaderAsync();
            return header.Level != level;
        }

        public void Dispose() => Rpc.Dispose();
    }

    public static class TezosNodeExt
    {
        public static void AddTezosNode(this IServiceCollection services)
        {
            services.AddSingleton<TezosNode>();
        }
    }
}
