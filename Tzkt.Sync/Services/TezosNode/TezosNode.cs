﻿using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using App.Metrics;
using Tzkt.Data;

namespace Tzkt.Sync.Services
{
    public sealed class TezosNode : IDisposable
    {
        public string BaseUrl { get; }
        readonly TezosNodeConfig Config;
        readonly TzktClient Rpc;
        readonly IServiceScopeFactory Services;
        readonly ILogger Logger;
        readonly IMetrics Metrics;

        Header? Header;
        Constants? Constants;
        DateTime NextBlock = DateTime.MinValue;
        DateTime NextSyncStateUpdate = DateTime.MinValue;

        public TezosNode(IServiceScopeFactory services, IConfiguration config, ILogger<TezosNode> logger, IMetrics metrics)
        {
            Config = config.GetTezosNodeConfig();
            BaseUrl = $"{Config.Endpoint.TrimEnd('/')}/";
            Rpc = new TzktClient(BaseUrl, Config.Timeout);
            Services = services;
            Logger = logger;
            Metrics = metrics;
        }

        public async Task<JsonElement> GetAsync(string url)
        {
            using var stream = await Rpc.GetStreamAsync(url);
            using var doc = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { MaxDepth = 100_000 });
            return doc.RootElement.Clone();
        }

        public async Task<T?> PostAsync<T>(string url, string content)
        {
            return await Rpc.PostAsync<T>(url, content);
        }

        public async Task<Header> GetHeaderAsync()
        {
            if (DateTime.UtcNow >= NextBlock)
            {
                var header = (await Rpc.GetObjectAsync<Header>(Config.Lag > 0
                    ? $"chains/main/blocks/head~{Config.Lag}/header"
                    : "chains/main/blocks/head/header"))!;

                if (header.Protocol != Header?.Protocol)
                    Constants = (await Rpc.GetObjectAsync<Constants>($"chains/main/blocks/{header.Level}/context/constants"))!;

                NextBlock = header.Level != Header?.Level
                    ? header.Timestamp.AddSeconds((Constants!.MinBlockDelay ?? Constants.BlockIntervals![0]) * (Config.Lag + 1))
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
                        syncTime = syncTime.AddTicks(-(syncTime.Ticks % 10_000_000));

                        await db.Database.ExecuteSqlRawAsync("""
                            UPDATE "AppState"
                            SET "KnownHead" = {0},
                                "LastSync" = {1}
                            """, header.Level, syncTime);
                        cache.AppState.UpdateSyncState(header.Level, syncTime);

                        Metrics.Measure.Gauge.SetHealthValue(cache.AppState.Get());

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

            return Header!;
        }

        public async Task<Header> GetHeaderAsync(int level)
        {
            return (await Rpc.GetObjectAsync<Header>($"chains/main/blocks/{level}/header"))!;
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
