using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Dapper;
using Npgsql;
using Netezos.Encoding;

namespace Tzkt.Sync.Services
{
    public class ContractMetadata : BackgroundService
    {
        readonly string ConnectionString;
        readonly ContractMetadataConfig Config;
        readonly ILogger Logger;
        readonly Regex Regex = new(@"(?<=(^|[^\\])(\\\\)*)\\u0000", RegexOptions.Compiled);

        ContractMetadataState State;

        public ContractMetadata(IConfiguration config, ILogger<ContractMetadata> logger)
        {
            ConnectionString = config.GetConnectionString("DefaultConnection");
            Config = config.GetContractMetadataConfig();
            Logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogInformation("Contract metadata started");

                await InitState();

                while (!stoppingToken.IsCancellationRequested)
                {
                    foreach (var dipdup in Config.DipDup)
                    {
                        try
                        {
                            #region sync dipdup
                            var state = await GetDipDupState(dipdup);
                            while (!stoppingToken.IsCancellationRequested)
                            {
                                Logger.LogDebug("Fetch dipdup updates from {url} @ {lastUpdateId}", dipdup.Url, state.LastUpdateId);

                                var updates = await GetDipDupMetadata(state.LastUpdateId, dipdup);
                                Logger.LogDebug("{cnt} updates received", updates.Count);

                                if (updates.Count > 0)
                                {
                                    var saved = await SaveContractMetadata(updates);
                                    Logger.LogDebug("{cnt} contracts updated", saved);

                                    if (saved != updates.Count)
                                    {
                                        Logger.LogWarning("Contract metadata is suspended until the indexer is in sync");
                                        break;
                                    }

                                    state.LastUpdateId = updates[^1].UpdateId;
                                    await SaveState();
                                    Logger.LogDebug("State for {url} updated with {lastUpdateId}", dipdup.Url, state.LastUpdateId);
                                }

                                if (updates.Count < dipdup.SelectLimit) break;
                            }
                            #endregion
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Failed to sync contract metadata for {url}", dipdup.Url);
                        }

                        if (stoppingToken.IsCancellationRequested)
                            break;
                    }

                    await Task.Delay(Config.Period * 1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Contract metadata crashed");
            }
            finally
            {
                Logger.LogInformation("Contract metadata stopped");
            }
        }

        async Task InitState()
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            var row = await conn.QueryFirstAsync("""
                SELECT "Extras"->'contractMetadata' as state
                FROM "AppState"
                WHERE "Id" = -1
                LIMIT 1
                """);
            
            try { State = row.state is string json ? JsonSerializer.Deserialize<ContractMetadataState>(json) : new(); }
            catch { State = new(); }

            foreach (var url in State.DipDup.Keys.Where(u => !Config.DipDup.Any(c => c.Url == u)).ToList())
                State.DipDup.Remove(url);

            foreach (var dipdup in Config.DipDup.Where(x => !State.DipDup.ContainsKey(x.Url)))
                State.DipDup.Add(dipdup.Url, new() { Sentinel = await GetDipDupSentinel(dipdup), LastUpdateId = 0 });
        }

        async Task SaveState()
        {
            var json = JsonSerializer.Serialize(State);
            using var conn = new NpgsqlConnection(ConnectionString);
            await conn.ExecuteAsync("""
                UPDATE "AppState"
                SET "Extras" = jsonb_set(COALESCE("Extras", '{}'), '{contractMetadata}', @json::jsonb)
                """, new { json });
        }

        async Task<ContractMetadataDipDupState> GetDipDupState(DipDupConfig dipdup)
        {
            var sentinel = await GetDipDupSentinel(dipdup);
            var state = State.DipDup[dipdup.Url];
            if (state.Sentinel != sentinel)
            {
                Logger.LogDebug("Sentinel changed {old} -> {new}, resetting DipDup state {url}",
                    state.Sentinel, sentinel, dipdup.Url);

                state.Sentinel = sentinel;
                state.LastUpdateId = 0;
            }
            return state;
        }

        async Task<int> SaveContractMetadata(List<DipDupItem> items)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            var saved = 0;
            var options = new JsonSerializerOptions { MaxDepth = 10240 };
            options.Converters.Add(new TokenMetadataConverter(9999));
            for (int i = 0; i < items.Count; i += 1000)
            {
                var sql = new StringBuilder();
                var param = new DynamicParameters();
                var max = Math.Min(1000, items.Count - i);

                sql.AppendLine(@"UPDATE ""Accounts"" SET ""Metadata"" = v.metadata FROM (VALUES");
                for (int j = 0; j < max; j++)
                {
                    var item = items[i + j];
                    if (j > 0) sql.Append(",\n");
                    param.Add($"@c{j}", item.Contract);
                    param.Add($"@m{j}", Regex.Replace(JsonSerializer.Serialize(item.Metadata, options), string.Empty));
                    sql.Append($"(@c{j}::varchar(37), @m{j}::jsonb)");
                }
                sql.AppendLine("\n) as v(contract, metadata)");
                sql.AppendLine(@"WHERE ""Address"" = v.contract");
                
                var query = sql.ToString();
                saved += await conn.ExecuteAsync(query, param);
            }
            return saved;
        }

        static async Task<string> GetDipDupSentinel(DipDupConfig dipdup)
        {
            using var client = new HttpClient();
            using var res = (await client.PostAsync(dipdup.Url, new StringContent(
                $"{{\"query\":\"query{{items:{dipdup.HeadStatusTable}(order_by:{{created_at:asc}},limit:1)" +
                $"{{created_at}}}}\",\"variables\":null}}",
                Encoding.UTF8, "application/json"))).EnsureSuccessStatusCode();

            var items = JsonSerializer.Deserialize<DipDupResponse<DipDupStatus>>(
                await res.Content.ReadAsStringAsync()).Data.Items;

            // There can be actually multiple status items (per each network), but it's ok:
            // 1. If new network is added there's no need to re-index from scratch
            // 2. If the oldest network is reset it means we need to re-fetch all the data (e.g. parsing issues fixed)
            // 3. Eventually we will reach the state when a single dipdup instance will be responsible for a single network
            return items[0].CreatedAt;
        }

        static async Task<List<DipDupItem>> GetDipDupMetadata(long lastUpdateId, DipDupConfig dipdup)
        {
            using var client = new HttpClient();
            using var res = (await client.PostAsync(dipdup.Url, new StringContent(
                $"{{\"query\":\"query{{items:{dipdup.MetadataTable}("
                + $"where:{{network:{{_eq:\\\"{dipdup.Network}\\\"}},metadata:{{_is_null:false}},"
                + $"update_id:{{_gt:\\\"{lastUpdateId}\\\"}}}},"
                + $"order_by:{{update_id:asc}},limit:{dipdup.SelectLimit})"
                + $"{{update_id contract metadata}}}}\",\"variables\":null}}",
                Encoding.UTF8, "application/json"))).EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                MaxDepth = 10240
            };

            return JsonSerializer.Deserialize<DipDupResponse<DipDupItem>>(
                Utf8.Parse(await res.Content.ReadAsStringAsync()), options).Data.Items;
        }

        class DipDupResponse<T>
        {
            [JsonPropertyName("data")]
            public DipDupQuery<T> Data { get; set; }
        }

        class DipDupQuery<T>
        {
            [JsonPropertyName("items")]
            public List<T> Items { get; set; } = new();
        }

        class DipDupItem
        {
            [JsonPropertyName("update_id")]
            public int UpdateId { get; set; }

            [JsonPropertyName("contract")]
            public string Contract { get; set; }

            [JsonPropertyName("metadata")]
            public JsonElement Metadata { get; set; }
        }

        class DipDupStatus
        {
            [JsonPropertyName("created_at")]
            public string CreatedAt { get; set; }
        }
    }
}
