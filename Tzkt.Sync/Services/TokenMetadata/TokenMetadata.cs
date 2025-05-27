using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        readonly JsonSerializerOptions OuterSerializerOptions;
        readonly JsonSerializerOptions InnerSerializerOptions;

        public TokenMetadata(IConfiguration config, ILogger<TokenMetadata> logger)
        {
            ConnectionString = config.GetDefaultConnectionString();
            Config = config.GetTokenMetadataConfig();
            Logger = logger;
            OuterSerializerOptions = new()
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                MaxDepth = 10240
            };
            InnerSerializerOptions = new()
            {
                MaxDepth = 1024
            };
            InnerSerializerOptions.Converters.Add(new TokenMetadataConverter());
        }

        TokenMetadataState State = null!;

        protected async Task SyncOverriddenMetadata()
        {
            if (Config.OverriddenMetadata?.Count > 0)
            {
                await SaveTokenMetadata(Config.OverriddenMetadata.Select(x => new DipDupItem
                {
                    UpdateId = 0,
                    Contract = x.Contract,
                    TokenId = x.TokenId,
                    Metadata = x.Metadata
                }).ToList());

                Logger.LogDebug("{cnt} overridden metadata initialized", Config.OverriddenMetadata.Count);
            }
        }

        protected async Task<int> SyncByUpdateId(DipDupConfig config, DipDupState state)
        {
            Logger.LogDebug("Fetch dipdup updates from {url} @ {lastUpdateId}", config.Url, state.LastUpdateId);

            var updates = await GetDipDupMetadata(state.LastUpdateId, config);
            Logger.LogDebug("{cnt} updates received", updates.Count);

            if (updates.Count > 0)
            {
                var cnt = await SaveTokenMetadata(updates);
                Logger.LogDebug("{cnt} tokens updated", cnt);

                state.LastUpdateId = updates[^1].UpdateId;
                await SaveState();
                Logger.LogDebug("Last update ID ({url}): {lastUpdateId})", config.Url, state.LastUpdateId);
            }

            return updates.Count;
        }

        protected async Task<int> SyncByLastTokenId(DipDupConfig config, DipDupState state)
        {
            Logger.LogDebug("Sync tokens since #{id}", state.LastTokenId);

            var tokens = await GetTokenIds(state.LastTokenId);
            Logger.LogDebug("{cnt} new tokens found", tokens.Count);

            if (tokens.Count > 0)
            {
                Logger.LogDebug("Fetch token metadata from {url} @ {lastTokenId}", config.Url, state.LastTokenId);
                var updates = await GetDipDupMetadata(tokens, config);
                Logger.LogDebug("{cnt} updates received", updates.Count);
                if (updates.Count > 0)
                {
                    var cnt = await SaveTokenMetadata(updates);
                    Logger.LogDebug("{cnt} tokens updated", cnt);
                }

                state.LastTokenId = tokens.Values.Max();
                await SaveState();
                Logger.LogDebug("Last token ID ({url}): {lastTokenId})", config.Url, state.LastTokenId);
            }

            return tokens.Count;
        }

        protected async Task<int> SyncByLastIndexedAt(DipDupConfig config, DipDupState state)
        {
            Logger.LogDebug("Sync tokens indexed since {level}", state.LastIndexedAt);

            var tokens = await GetTokenIds(state.LastIndexedAt, state.LastIndexedAtId);
            Logger.LogDebug("{cnt} new tokens found", tokens.Count);

            if (tokens.Count > 0)
            {
                Logger.LogDebug("Fetch token metadata from {url} @ {level}:{id}",
                    config.Url, state.LastIndexedAt, state.LastIndexedAtId);

                var updates = await GetDipDupMetadata(tokens, config);
                Logger.LogDebug("{cnt} updates received", updates.Count);
                if (updates.Count > 0)
                {
                    var cnt = await SaveTokenMetadata(updates);
                    Logger.LogDebug("{cnt} tokens updated", cnt);
                }

                (state.LastIndexedAt, state.LastIndexedAtId) = tokens.Values
                    .OrderByDescending(x => x.Item1)
                    .ThenByDescending(x => x.Item2)
                    .First();

                await SaveState();

                Logger.LogDebug("Last indexed at ({url}): {level}:{id})",
                    config.Url, state.LastIndexedAt, state.LastIndexedAtId);
            }

            return tokens.Count;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogInformation("Token metadata started");

                await InitState();
                await SyncOverriddenMetadata();

                while (!stoppingToken.IsCancellationRequested)
                {
                    foreach (var config in Config.DipDup)
                    {
                        try
                        {
                            var state = await GetDipDupState(config);

                            while (!stoppingToken.IsCancellationRequested)
                            {
                                var updatesCount = await SyncByUpdateId(config, state);
                                if (updatesCount < config.SelectLimit)
                                    break;
                            }

                            while (!stoppingToken.IsCancellationRequested)
                            {
                                var tokensCount = await SyncByLastTokenId(config, state);
                                if (tokensCount < Config.BatchSize)
                                    break;
                            }

                            while (!stoppingToken.IsCancellationRequested)
                            {
                                var tokensCount = await SyncByLastIndexedAt(config, state);
                                if (tokensCount < Config.BatchSize)
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Failed to sync token metadata for {url}", config.Url);
                        }

                        if (stoppingToken.IsCancellationRequested)
                            break;
                    }

                    await Task.Delay(Config.PeriodSec * 1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Token metadata crashed");
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
                SELECT ""Extras""->'tokenMetadata' as state, ""OperationCounter"", ""Level""
                FROM ""AppState""
                WHERE ""Id"" = -1
                LIMIT 1");
            
            try { State = row.state is string json ? JsonSerializer.Deserialize<TokenMetadataState>(json)! : new(); }
            catch { State = new(); }  // will catch parse errors and reset the state (handle State model changes)

            foreach (var url in State.DipDup.Keys.Where(u => !Config.DipDup.Any(c => c.Url == u)).ToList())
                State.DipDup.Remove(url);

            foreach (var config in Config.DipDup)
            {
                if (!State.DipDup.TryGetValue(config.Url, out var state))
                {
                    state = new();
                    State.DipDup.Add(config.Url, state);
                }

                if (state.LastUpdateId == 0)  // If we are syncing from scratch, we can safely assume there will be no races
                {
                    state.LastTokenId = (row.OperationCounter + 1) << Data.Models.AppState.SubIdBits;
                    state.LastIndexedAt = row.Level;
                    state.LastIndexedAtId = state.LastTokenId;
                }

                Logger.LogDebug("DipDup state ({url}): LastUpdateId={updateId}, LastTokenId={tokenId}, LastIndexedAt={indexedAt}, LastIndexedAtId={indexedAtId}",
                    config.Url, state.LastUpdateId, state.LastTokenId, state.LastIndexedAt, state.LastIndexedAtId);
            }

            await SaveState();
        }

        async Task<DipDupState> GetDipDupState(DipDupConfig config)
        {
            var sentinel = await GetDipDupSentinel(config);
            var state = State.DipDup[config.Url];
            if (state.Sentinel != sentinel)
            {
                Logger.LogDebug("Sentinel changed {old} -> {new}, resetting DipDup state {url}",
                    state.Sentinel, sentinel, config.Url);

                using var conn = new NpgsqlConnection(ConnectionString);
                var row = await conn.QueryFirstAsync(@"
                    SELECT ""OperationCounter"", ""Level""
                    FROM ""AppState""
                    WHERE ""Id"" = -1
                    LIMIT 1");

                state.LastUpdateId = 0;
                state.LastTokenId = (row.OperationCounter + 1) << Data.Models.AppState.SubIdBits;
                state.LastIndexedAt = row.Level;
                state.LastIndexedAtId = state.LastTokenId;
                state.Sentinel = sentinel;
            }
            return state;
        }

        async Task SaveState()
        {
            var json = JsonSerializer.Serialize(State);
            using var conn = new NpgsqlConnection(ConnectionString);
            await conn.ExecuteAsync($@"
                UPDATE ""AppState""
                SET ""Extras"" = jsonb_set(COALESCE(""Extras"", '{{}}'), '{{tokenMetadata}}', '{json}')");
        }

        static async Task<Dictionary<string, int>> GetContractIds(NpgsqlConnection conn, List<string> addresses)
        {
            return (await conn.QueryAsync(@"
                SELECT ""Id"", ""Address""
                FROM ""Accounts""
                WHERE ""Address"" = ANY(@addresses::varchar(37)[])",
                new { addresses }))
                .ToDictionary(x => (string)x.Address, x => (int)x.Id);
        }

        async Task<Dictionary<(string, string), (int, long)>> GetTokenIds(int indexedAt, long indexedAtId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            return (await conn.QueryAsync($@"
                SELECT t.""Id"", c.""Address"", t.""TokenId""::text, t.""IndexedAt""
                FROM ""Tokens"" as t
                INNER JOIN ""Accounts"" as c
                ON c.""Id"" = t.""ContractId""
                WHERE t.""IndexedAt"" IS NOT NULL
                AND   (t.""IndexedAt"" = {indexedAt} AND t.""Id"" > {indexedAtId} OR t.""IndexedAt"" > {indexedAt})
                ORDER BY t.""IndexedAt"", t.""Id""
                LIMIT {Config.BatchSize}"))
                .ToDictionary(x => ((string)x.Address, (string)x.TokenId), x => ((int)x.IndexedAt, (long)x.Id));
        }

        async Task<Dictionary<(string, string), long>> GetTokenIds(long lastId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            return (await conn.QueryAsync($@"
                SELECT t.""Id"", c.""Address"", t.""TokenId""::text
                FROM ""Tokens"" as t
                INNER JOIN ""Accounts"" as c
                ON c.""Id"" = t.""ContractId""
                WHERE t.""Id"" > {lastId}
                ORDER BY t.""Id""
                LIMIT {Config.BatchSize}"))
                .ToDictionary(x => ((string)x.Address, (string)x.TokenId), x => (long)x.Id);
        }

        async Task<string> GetDipDupSentinel(DipDupConfig dipDupConfig)
        {
            using var client = new HttpClient();
            using var res = (await client.PostAsync(dipDupConfig.Url, new StringContent(
                $"{{\"query\":\"query{{items:{dipDupConfig.HeadStatusTable}(order_by:{{created_at:asc}},limit:1)"
                + $"{{created_at}}}}\",\"variables\":null}}",
                Encoding.UTF8, "application/json"))).EnsureSuccessStatusCode();

            var items = JsonSerializer.Deserialize<DipDupResponse<DipDupStatus>>(
                await res.Content.ReadAsStringAsync())!.Data.Items;

            // There can be actually multiple status items (per each network), but it's ok:
            // 1. If new network is added there's no need to re-index from scratch
            // 2. If the oldest network is reset it means we need to re-fetch all the data (e.g. parsing issues fixed)
            // 3. Eventually we will reach the state when a single dipdup instance will be responsible for a single network
            return items[0].CreatedAt;
        }

        async Task<List<DipDupItem>> GetDipDupMetadata(long lastUpdateId, DipDupConfig dipDupConfig)
        {
            var filterByContract = dipDupConfig.Filter is not DipDupFilter filter
                ? string.Empty
                : filter.Mode == DipDupFilter.FilterMode.Exclude
                    ? $",contract:{{_nin:[{string.Join(',', filter.Contracts.Select(x => $"\\\"{x}\\\""))}]}}"
                    : $",contract:{{_in:[{string.Join(',', filter.Contracts.Select(x => $"\\\"{x}\\\""))}]}}";

            using var client = new HttpClient();
            using var res = (await client.PostAsync(dipDupConfig.Url, new StringContent(
                $"{{\"query\":\"query{{items:{dipDupConfig.MetadataTable}("
                + $"where:{{network:{{_eq:\\\"{dipDupConfig.Network}\\\"}},update_id:{{_gt:\\\"{lastUpdateId}\\\"}}"
                + $"{filterByContract}}},"
                + $"order_by:{{update_id:asc}},limit:{dipDupConfig.SelectLimit})"
                + $"{{update_id contract token_id metadata}}}}\",\"variables\":null}}",
                Encoding.UTF8, "application/json"))).EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<DipDupResponse<DipDupItem>>(
                Utf8.Parse(await res.Content.ReadAsStringAsync()), OuterSerializerOptions)!.Data.Items;
        }

        async Task<List<DipDupItem>> GetDipDupMetadata<T>(Dictionary<(string, string), T> tokens, DipDupConfig dipDupConfig)
        {
            var keys = dipDupConfig.Filter is not DipDupFilter filter
                ? tokens.Keys
                : filter.Mode == DipDupFilter.FilterMode.Exclude
                    ? tokens.Keys.Where(k => !filter.Contracts.Contains(k.Item1))
                    : tokens.Keys.Where(k => filter.Contracts.Contains(k.Item1));

            if (!keys.Any())
                return [];

            var contracts = string.Join(',', keys.Select(x => $"\\\"{x.Item1}\\\"").Distinct());
            var tokenIds = string.Join(',', keys.Select(x => $"\\\"{x.Item2}\\\"").Distinct());
            var items = new List<DipDupItem>(tokens.Count);

            using var client = new HttpClient();
            var lastUpdateId = -1L;
            while (true)
            {
                using var res = (await client.PostAsync(dipDupConfig.Url, new StringContent(
                    $"{{\"query\":\"query{{items:{dipDupConfig.MetadataTable}("
                    + $"where:{{network:{{_eq:\\\"{dipDupConfig.Network}\\\"}},update_id:{{_gt:\\\"{lastUpdateId}\\\"}},"
                    + $"contract:{{_in:[{contracts}]}},token_id:{{_in:[{tokenIds}]}}}},"
                    + $"order_by:{{update_id:asc}},limit:{dipDupConfig.SelectLimit})"
                    + $"{{update_id contract token_id metadata}}}}\",\"variables\":null}}",
                    Encoding.UTF8, "application/json"))).EnsureSuccessStatusCode();

                var _items = JsonSerializer.Deserialize<DipDupResponse<DipDupItem>>(
                    Utf8.Parse(await res.Content.ReadAsStringAsync()), OuterSerializerOptions)!.Data.Items;

                items.AddRange(_items.Where(x => tokens.ContainsKey((x.Contract, x.TokenId))));
                if (_items.Count < dipDupConfig.SelectLimit) break;
                lastUpdateId = _items[^1].UpdateId;
            }
            return items;
        }

        async Task<int> SaveTokenMetadata(List<DipDupItem> items)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            var contracts = await GetContractIds(conn, items.Select(x => x.Contract).ToHashSet().ToList());
            if (contracts.Count == 0) return 0;
            var saved = 0;
            for (int i = 0; i < items.Count; i += 1000)
            {
                var any = false;
                var sql = new StringBuilder();
                var param = new DynamicParameters();
                var max = Math.Min(1000, items.Count - i);

                sql.AppendLine(@"UPDATE ""Tokens"" SET ""Metadata"" = v.metadata FROM (VALUES");
                for (int j = 0; j < max; j++)
                {
                    var item = items[i + j];
                    if (contracts.TryGetValue(item.Contract, out var contractId))
                    {
                        if (any) sql.AppendLine(",");
                        else any = true;
                        var metadata = item.Metadata is JsonElement json
                            ? Regexes.Metadata().Replace(
                                JsonSerializer.Serialize(json, InnerSerializerOptions),
                                string.Empty)
                            : null;
                        param.Add($"@p{j}", metadata);
                        sql.Append($"({contractId}, '{item.TokenId}'::numeric, @p{j}::jsonb)");
                    }
                }
                sql.AppendLine();
                sql.AppendLine(@") as v(contract, token, metadata)");
                sql.AppendLine(@"WHERE ""ContractId"" = v.contract AND ""TokenId"" = v.token");

                if (any) saved += await conn.ExecuteAsync(sql.ToString(), param);
            }
            return saved;
        }

        class DipDupResponse<T>
        {
            [JsonPropertyName("data")]
            public required DipDupQuery<T> Data { get; set; }
        }

        class DipDupQuery<T>
        {
            [JsonPropertyName("items")]
            public required List<T> Items { get; set; }
        }

        class DipDupItem
        {
            [JsonPropertyName("update_id")]
            public required long UpdateId { get; set; }

            [JsonPropertyName("contract")]
            public required string Contract { get; set; }

            [JsonPropertyName("token_id")]
            public required string TokenId { get; set; }

            [JsonPropertyName("metadata")]
            public required JsonElement? Metadata { get; set; }
        }

        class DipDupStatus
        {
            [JsonPropertyName("created_at")]
            public required string CreatedAt { get; set; }
        }
    }
}
