using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Dapper;
using Npgsql;
using Netezos.Encoding;

namespace Tzkt.Sync.Services
{
    public class TokenMetadata : BackgroundService
    {
        readonly string ConnectionString;
        readonly TokenMetadataConfig Config;
        readonly ILogger Logger;

        TokenMetadataState State;

        public TokenMetadata(IConfiguration config, ILogger<TokenMetadata> logger)
        {
            ConnectionString = config.GetConnectionString("DefaultConnection");
            Config = config.GetTokenMetadataConfig();
            Logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogInformation("Token metadata started");

                await InitState();

                Logger.LogDebug("Token metadata initialized with ({dipDupCount} dipdup states, internal token id -> {tokenId})",
                    State.DipDup.Count(), State.LastTokenId);

                if (Config.OverriddenMetadata?.Count > 0)
                {
                    await SaveTokenMetadata(Config.OverriddenMetadata.Select(x => new DipDupItem
                    {
                        Contract = x.Contract,
                        TokenId = x.TokenId,
                        Metadata = x.Metadata
                    }).ToList());

                    Logger.LogDebug("{cnt} overridden metadata initialized", Config.OverriddenMetadata.Count);
                }

                Dictionary<(string, string), int> tokens;
                List<DipDupItem> updates;
                int cnt;

                while (!stoppingToken.IsCancellationRequested)
                {
                    #region fetch latest DipDup metadata and enrich existing TzKT tokens only
                    foreach (var config in Config.DipDup)
                    {
                        try
                        {
                            DipDupState state = await GetDipDupState(config);
                            while (!stoppingToken.IsCancellationRequested)
                            {
                                Logger.LogDebug("Fetch dipdup updates from {url} @ {lastUpdateId}",
                                    config.Url, state.LastUpdateId);

                                updates = await GetDipDupMetadata(state.LastUpdateId, config);
                                Logger.LogDebug("{cnt} updates received", updates.Count);
                                if (updates.Count == 0) break;

                                cnt = await SaveTokenMetadata(updates);
                                Logger.LogDebug("{cnt} tokens updated", cnt);

                                state.LastUpdateId = updates[^1].UpdateId;
                                State.DipDup[config.Url] = state;
                                await SaveState();
                                Logger.LogDebug("State: {url} @ {lastUpdateId}",
                                    config.Url, state.LastUpdateId);

                                if (updates.Count < Config.BatchSize) break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogTrace(ex, "Failed to sync token metadata");
                        }
                    }
                    #endregion

                    if (stoppingToken.IsCancellationRequested)
                        break;

                    #region try to fetch DipDup metadata for new TzKT tokens (in case we missed it earlier)
                    try
                    {                    
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            Logger.LogDebug("Sync tokens since #{id}", State.LastTokenId);
                            tokens = await GetTokenIds(State.LastTokenId);
                            Logger.LogDebug("{cnt} new tokens found", tokens.Count);
                            if (tokens.Count == 0) break;

                            foreach (var config in Config.DipDup)
                            {
                                Logger.LogDebug("Fetch token metadata from {url}", config.Url);
                                updates = await GetDipDupMetadata(tokens, config);
                                Logger.LogDebug("{cnt} updates received", updates.Count);
                                if (updates.Count > 0)
                                {
                                    cnt = await SaveTokenMetadata(updates);
                                    Logger.LogDebug("{cnt} tokens updated", cnt);
                                }
                            }

                            State.LastTokenId = tokens.Values.Max();
                            await SaveState();
                            Logger.LogDebug("State: internal token id -> {lastTokenId}", State.LastTokenId);

                            if (tokens.Count < Config.BatchSize) break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogTrace(ex, "Failed to sync token metadata");
                    }
                    #endregion

                    await Task.Delay(Config.PeriodSec * 1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrace(ex, "Token metadata crashed");
            }
            finally
            {
                Logger.LogInformation("Token metadata stopped");
            }
        }

        async Task InitState()
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            var row = await conn.QueryFirstAsync(@"
                SELECT ""Metadata""->'tokenMetadata' as state, ""TokenCounter""
                FROM ""AppState""
                WHERE ""Id"" = -1
                LIMIT 1");
            
            try { State = row.state is string json ? JsonSerializer.Deserialize<TokenMetadataState>(json) : new(); }
            catch { State = new(); }  // will catch parse errors and reset the state (handle State model changes)

            if (State.LastTokenId == 0)
                State.LastTokenId = row.TokenCounter;  // Internal TzKT token Id
        }

        async Task<DipDupState> GetDipDupState(DipDupConfig config)
        {
            string Sentinel = await GetDipDupSentinel(config);
            DipDupState state = State.DipDup.GetValueOrDefault(config.Url, new());          
            if (state.Sentinel != Sentinel)
            {
                Logger.LogDebug("Sentinel changed {old} -> {new}, resetting DipDup source {url}",
                    state.Sentinel, Sentinel, config.Url);
                state.LastUpdateId = -1;
                state.Sentinel = Sentinel;
            }
            return state;
        }

        async Task SaveState()
        {
            var json = JsonSerializer.Serialize(State);
            using var conn = new NpgsqlConnection(ConnectionString);
            await conn.ExecuteAsync($@"
                UPDATE ""AppState""
                SET ""Metadata"" = jsonb_set(COALESCE(""Metadata"", '{{}}'), '{{tokenMetadata}}', '{json}')");
        }

        static async Task<Dictionary<string, int>> GetContractIds(NpgsqlConnection conn, List<string> addresses)
        {
            return (await conn.QueryAsync(@"
                SELECT ""Id"", ""Address""
                FROM ""Accounts""
                WHERE ""Address"" = ANY(@addresses::character(36)[])",
                new { addresses }))
                .ToDictionary(x => (string)x.Address, x => (int)x.Id);
        }

        async Task<Dictionary<(string, string), int>> GetTokenIds(int lastId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            return (await conn.QueryAsync($@"
                SELECT t.""Id"", c.""Address"", t.""TokenId""
                FROM ""Tokens"" as t
                INNER JOIN ""Accounts"" as c
                ON c.""Id"" = t.""ContractId""
                WHERE t.""Id"" > {lastId}
                ORDER BY t.""Id""
                LIMIT {Config.BatchSize}"))
                .ToDictionary(x => ((string)x.Address, (string)x.TokenId), x => (int)x.Id);
        }

        async Task<string> GetDipDupSentinel(DipDupConfig dipDupConfig)
        {
            using var client = new HttpClient();
            using var res = (await client.PostAsync(dipDupConfig.Url, new StringContent(
                $"{{\"query\":\"query{{items:{dipDupConfig.HeadStatusTable}(limit:1)"
                + $"{{created_at}}}}\",\"variables\":null}}",
                Encoding.UTF8, "application/json"))).EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                MaxDepth = 10240
            };

            var items = (await JsonSerializer.DeserializeAsync<DipDupResponse<DipDupStatus>>(
                await res.Content.ReadAsStreamAsync(), options)).Data.Items;

            // there can be actually multiple status items (per each network), but it's ok
            return items[0].CreatedAt;
        }

        async Task<List<DipDupItem>> GetDipDupMetadata(int lastUpdateId, DipDupConfig dipDupConfig)
        {
            using var client = new HttpClient();
            using var res = (await client.PostAsync(dipDupConfig.Url, new StringContent(
                $"{{\"query\":\"query{{items:{dipDupConfig.MetadataTable}("
                + $"where:{{network:{{_eq:\\\"{dipDupConfig.Network}\\\"}},metadata:{{_is_null:false}},"
                + $"update_id:{{_gt:\\\"{lastUpdateId}\\\"}}}},"
                + $"order_by:{{update_id:asc}},limit:{Config.BatchSize})"
                + $"{{update_id contract token_id metadata}}}}\",\"variables\":null}}",
                Encoding.UTF8, "application/json"))).EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                MaxDepth = 10240
            };

            return (await JsonSerializer.DeserializeAsync<DipDupResponse<DipDupItem>>(
                await res.Content.ReadAsStreamAsync(), options)).Data.Items;
        }

        async Task<List<DipDupItem>> GetDipDupMetadata(Dictionary<(string, string), int> tokens, DipDupConfig dipDupConfig)
        {
            var contracts = string.Join(',', tokens.Keys.Select(x => $"\\\"{x.Item1}\\\"").Distinct());
            var tokenIds = string.Join(',', tokens.Keys.Select(x => $"\\\"{x.Item2}\\\"").Distinct());
            var items = new List<DipDupItem>(tokens.Count);

            using var client = new HttpClient();
            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                MaxDepth = 10240
            };
            var lastUpdateId = 0;
            while (true)
            {
                using var res = (await client.PostAsync(dipDupConfig.Url, new StringContent(
                    $"{{\"query\":\"query{{items:{dipDupConfig.MetadataTable}("
                    + $"where:{{network:{{_eq:\\\"{dipDupConfig.Network}\\\"}},metadata:{{_is_null:false}},"
                    + $"update_id:{{_gt:\\\"{lastUpdateId}\\\"}},contract:{{_in:[{contracts}]}},token_id:{{_in:[{tokenIds}]}}}},"
                    + $"order_by:{{update_id:asc}},limit:{Config.BatchSize})"
                    + $"{{update_id contract token_id metadata}}}}\",\"variables\":null}}",
                    Encoding.UTF8, "application/json"))).EnsureSuccessStatusCode();

                var _items = (await JsonSerializer.DeserializeAsync<DipDupResponse<DipDupItem>>(
                    await res.Content.ReadAsStreamAsync(), options)).Data.Items;

                items.AddRange(_items.Where(x => tokens.ContainsKey((x.Contract, x.TokenId))));
                if (_items.Count < Config.BatchSize) break;
                lastUpdateId = _items[^1].UpdateId;
            }
            return items;
        }

        async Task<int> SaveTokenMetadata(List<DipDupItem> items)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            var contracts = await GetContractIds(conn, items.Select(x => x.Contract).ToHashSet().ToList());
            var saved = 0;
            var options = new JsonSerializerOptions { MaxDepth = 1024 };
            options.Converters.Add(new TokenMetadataConverter());
            for (int i = 0; i < items.Count; i += 1000)
            {
                var comma = false;
                var sql = new StringBuilder();
                var param = new DynamicParameters();
                var max = Math.Min(1000, items.Count - i);

                sql.AppendLine(@"UPDATE ""Tokens"" SET ""Metadata"" = v.metadata FROM (VALUES");
                for (int j = 0; j < max; j++)
                {
                    var item = items[i + j];
                    if (contracts.TryGetValue(item.Contract, out var contractId))
                    {
                        if (comma) sql.AppendLine(",");
                        else comma = true;
                        param.Add($"@p{j}", JsonSerializer.Serialize(item.Metadata, options));
                        sql.Append($"({contractId}, '{item.TokenId}', @p{j}::jsonb)");
                    }
                }
                sql.AppendLine();
                sql.AppendLine(@") as v(contract, token, metadata)");
                sql.AppendLine(@"WHERE ""ContractId"" = v.contract AND ""TokenId"" = v.token");

                saved += await conn.ExecuteAsync(sql.ToString(), param);
            }
            return saved;
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

            [JsonPropertyName("token_id")]
            public string TokenId { get; set; }

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
