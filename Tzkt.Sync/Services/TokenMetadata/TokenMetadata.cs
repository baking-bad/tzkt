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
        const string TDAddress = "KT1GBZmSxmnKJXGMdMLbugPfLyUPmuLSMwKS";

        readonly string ConnectionString;
        readonly TokenMetadataConfig Config;
        readonly ILogger Logger;

        TokenMetadataState State;
        int TDBigMap;

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

                Logger.LogDebug("Token metadata initialized with ({dipDupId}, {tokenId}, {tdId}, {tdBigMap})",
                    State.LastDipDupId, State.LastTokenId, State.LastTDId, TDBigMap);

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
                    try
                    {
                        #region sync tezos domains
                        if (TDBigMap != 0)
                        {
                            while (!stoppingToken.IsCancellationRequested)
                            {
                                Logger.LogDebug("Fetch tezos domains updates since #{id}", State.LastTDId);
                                updates = await GetTDMetadata(State.LastTDId);
                                Logger.LogDebug("{cnt} updates received", updates.Count);
                                if (updates.Count == 0) break;

                                cnt = await SaveTokenMetadata(updates);
                                Logger.LogDebug("{cnt} tokens updated", cnt);

                                State.LastTDId = updates[^1].UpdateId;
                                await SaveState();
                                Logger.LogDebug("State: ({dipDupId}, {tokenId}, {tdId})",
                                    State.LastDipDupId, State.LastTokenId, State.LastTDId);

                                if (updates.Count < Config.BatchSize) break;
                            }
                        }
                        #endregion

                        if (stoppingToken.IsCancellationRequested)
                            break;

                        #region sync dipdup
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            Logger.LogDebug("Fetch dipdup updates since #{id}", State.LastDipDupId);
                            updates = await GetDipDupMetadata(State.LastDipDupId);
                            Logger.LogDebug("{cnt} updates received", updates.Count);
                            if (updates.Count == 0) break;

                            cnt = await SaveTokenMetadata(updates);
                            Logger.LogDebug("{cnt} tokens updated", cnt);

                            State.LastDipDupId = updates[^1].UpdateId;
                            await SaveState();
                            Logger.LogDebug("State: ({dipDupId}, {tokenId}, {tdId})",
                                State.LastDipDupId, State.LastTokenId, State.LastTDId);

                            if (updates.Count < Config.BatchSize) break;
                        }
                        #endregion

                        if (stoppingToken.IsCancellationRequested)
                            break;

                        #region sync new tokens
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            Logger.LogDebug("Sync tokens since #{id}", State.LastTokenId);
                            tokens = await GetTokenIds(State.LastTokenId);
                            Logger.LogDebug("{cnt} new tokens found", tokens.Count);
                            if (tokens.Count == 0) break;

                            Logger.LogDebug("Fetch token metadata");
                            updates = await GetDipDupMetadata(tokens);
                            Logger.LogDebug("{cnt} updates received", updates.Count);
                            if (updates.Count > 0)
                            {
                                cnt = await SaveTokenMetadata(updates);
                                Logger.LogDebug("{cnt} tokens updated", cnt);
                            }

                            State.LastTokenId = tokens.Values.Max();
                            await SaveState();
                            Logger.LogDebug("State: ({dipDupId}, {tokenId}, {tdId})",
                                State.LastDipDupId, State.LastTokenId, State.LastTDId);

                            if (tokens.Count < Config.BatchSize) break;
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Failed to sync token metadata: {msg}", ex.Message);
                    }
                    await Task.Delay(Config.PeriodSec * 1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Token metadata crashed: {msg}", ex.Message);
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
            catch { State = new(); }

            if (State.LastDipDupId == 0)
                State.LastTokenId = row.TokenCounter;

            var tdContractId = await conn.QueryFirstOrDefaultAsync<int>($@"
                SELECT ""Id""
                FROM ""Accounts""
                WHERE ""Address"" = '{TDAddress}'
                LIMIT 1");

            if (tdContractId != 0)
            {
                TDBigMap = await conn.QueryFirstOrDefaultAsync<int>($@"
                    SELECT ""Ptr""
                    FROM ""BigMaps""
                    WHERE ""ContractId"" = {tdContractId}
                    AND ""StoragePath"" = 'store.tzip12_tokens'
                    LIMIT 1");
            }
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

        async Task<List<DipDupItem>> GetDipDupMetadata(int lastUpdateId)
        {
            using var client = new HttpClient();
            using var res = (await client.PostAsync(Config.DipDupUrl, new StringContent(
                $"{{\"query\":\"query{{token_metadata(where:{{network:{{_eq:\\\"{Config.Network}\\\"}},"
                + $"update_id:{{_gt:\\\"{lastUpdateId}\\\"}}}},order_by:{{update_id:asc}},limit:{Config.BatchSize})"
                + $"{{update_id contract token_id metadata}}}}\",\"variables\":null}}",
                Encoding.UTF8, "application/json"))).EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                MaxDepth = 10240
            };

            return (await JsonSerializer.DeserializeAsync<DipDupResponse>(
                await res.Content.ReadAsStreamAsync(), options)).Data.Items;
        }

        async Task<List<DipDupItem>> GetDipDupMetadata(Dictionary<(string, string), int> tokens)
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
                using var res = (await client.PostAsync(Config.DipDupUrl, new StringContent(
                    $"{{\"query\":\"query{{token_metadata(where:{{network:{{_eq:\\\"{Config.Network}\\\"}},"
                    + $"update_id:{{_gt:\\\"{lastUpdateId}\\\"}},contract:{{_in:[{contracts}]}},token_id:{{_in:[{tokenIds}]}}}},"
                    + $"order_by:{{update_id:asc}},limit:{Config.BatchSize}){{update_id contract token_id metadata}}}}\",\"variables\":null}}",
                    Encoding.UTF8, "application/json"))).EnsureSuccessStatusCode();

                var _items = (await JsonSerializer.DeserializeAsync<DipDupResponse>(
                    await res.Content.ReadAsStreamAsync(), options)).Data.Items;

                items.AddRange(_items.Where(x => tokens.ContainsKey((x.Contract, x.TokenId))));
                if (_items.Count < Config.BatchSize) break;
                lastUpdateId = _items[^1].UpdateId;
            }
            return items;
        }

        async Task<List<DipDupItem>> GetTDMetadata(int lastId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            return (await conn.QueryAsync($@"
                SELECT u.""Id"", k.""JsonKey"", u.""JsonValue""
                FROM ""BigMapUpdates"" as u
                LEFT JOIN ""BigMapKeys"" as k ON k.""Id"" = u.""BigMapKeyId""
                WHERE u.""BigMapPtr"" = {TDBigMap}
                AND u.""Id"" > {lastId}
                AND u.""Action"" IN (1, 2)
                ORDER BY u.""Id""
                LIMIT {Config.BatchSize}"))
                .Select(x =>
                {
                    var domain = Utf8.Convert(Hex.Parse(((string)x.JsonValue)[1..^1]));
                    return new DipDupItem
                    {
                        UpdateId = (int)x.Id,
                        Contract = TDAddress,
                        TokenId = ((string)x.JsonKey)[1..^1],
                        Metadata = JsonSerializer.Deserialize<JsonElement>(
                            $@"{{""name"":""{domain}"",""symbol"":""TD"",""decimals"":""0"",""isBooleanAmount"":true}}")
                    };
                })
                .ToList();
        }

        async Task<int> SaveTokenMetadata(List<DipDupItem> items)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            var contracts = await GetContractIds(conn, items.Select(x => x.Contract).ToHashSet().ToList());
            var saved = 0;
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                NumberHandling = JsonNumberHandling.WriteAsString,
                MaxDepth = 10240
            };
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

        class DipDupResponse
        {
            [JsonPropertyName("data")]
            public DipDupQuery Data { get; set; }
        }

        class DipDupQuery
        {
            [JsonPropertyName("token_metadata")]
            public List<DipDupItem> Items { get; set; } = new();
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
    }
}
