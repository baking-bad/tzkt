using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services
{
    public sealed class MavrykNode : IDisposable
    {
        public string BaseUrl { get; }
        readonly MavrykNodeConfig Config;
        readonly TzktClient Rpc;
        readonly IServiceScopeFactory Services;
        readonly ILogger Logger;

        Header Header;
        Constants Constants;
        DateTime NextBlock;
        DateTime NextSyncStateUpdate;

        public MavrykNode(IServiceScopeFactory services, IConfiguration config, ILogger<MavrykNode> logger)
        {
            Config = config.GetMavrykNodeConfig();
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

        public async Task<T> PostAsync<T>(string url, string content)
        {
            return await Rpc.PostAsync<T>(url, content);
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

    public static class MavrykNodeExt
    {
        public static void AddMavrykNode(this IServiceCollection services)
        {
            services.AddSingleton<MavrykNode>();
        }
    }
}
