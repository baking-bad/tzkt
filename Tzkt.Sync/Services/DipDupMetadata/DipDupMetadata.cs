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

namespace Tzkt.Sync.Services
{
    public class DipDupMetadata : BackgroundService
    {
        #region legacy token metadata
        readonly static List<(string, string)> LegacyTokens = new()
        {
            ("KT1PWx2mnDueood7fEmfbBDKx1D9BAnnXitn", @"{""name"":""tzBTC"",""symbol"":""tzBTC"",""decimals"":8}"),
            ("KT1VYsVfmobT7rsMVivvZ4J8i3bPiqz12NaH", @"{""name"":""wXTZ"",""symbol"":""wXTZ"",""decimals"":6}"),
            ("KT1LN4LPSqTMS7Sd2CJw4bbDGRkMv2t68Fy9", @"{""name"":""USDtez"",""symbol"":""USDtz"",""decimals"":6}"),
            ("KT19at7rQUvyjxnZ2fBv7D9zc8rkyG7gAoU8", @"{""name"":""ETHtez"",""symbol"":""ETHtz"",""decimals"":18}"),
            ("KT1REEb5VxWRjcHm5GzDMwErMmNFftsE5Gpf", @"{""name"":""Stably USD"",""symbol"":""USDS"",""decimals"":6}"),
            ("KT1AEfeckNbdEYwaMKkytBwPJPycz7jdSGea", @"{""name"":""STKR"",""symbol"":""STKR"",""decimals"":18}"),
            ("KT1AafHA1C1vk959wvHWBispY9Y2f3fxBUUo", @"{""name"":""LB Token"",""symbol"":""LBT"",""decimals"":0}"),
            ("KT1K9gCRgaLRFKTErYt1wVxA3Frb9FjasjTV", @"{""name"":""Kolibri USD"",""symbol"":""kUSD"",""decimals"":18}"),
            ("KT1AFA2mwNUMNd4SsujE1YYp29vd8BZejyKW", @"{""name"":""Hic et nunc DAO"",""symbol"":""hDAO"",""decimals"":6}")
        };
        #endregion

        readonly string ConnectionString;
        readonly DipDupMetadataConfig Config;
        readonly ILogger Logger;

        DipDupMetadataState State;

        public DipDupMetadata(IConfiguration config, ILogger<DipDupMetadata> logger)
        {
            ConnectionString = config.GetConnectionString("DefaultConnection");
            Config = config.GetDipDupMetadataConfig();
            Logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogInformation("DipDup metadata started");

                await InitState();
                Logger.LogDebug("DupDup metadata initialized with ({updateId}, {tokenId})",
                    State.LastUpdateId, State.LastTokenId);

                var cnt = await InitLegacyTokens();
                if (cnt > 0) Logger.LogDebug("{cnt} legacy tokens initialized", cnt);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        #region fetch updates
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            Logger.LogDebug("Fetch token metadata updates since #{id}", State.LastUpdateId);
                            var updates = await FetchTokenMetadata(State.LastUpdateId);
                            Logger.LogDebug("{cnt} updates received", updates.Count);
                            if (updates.Count == 0) break;

                            cnt = await SaveTokenMetadata(updates);
                            Logger.LogDebug("{cnt} tokens updated", cnt);

                            State.LastUpdateId = updates[^1].UpdateId;
                            await SaveState();
                            Logger.LogDebug("State: ({updateId}, {tokenId})", State.LastUpdateId, State.LastTokenId);

                            if (updates.Count < Config.BatchSize) break;
                        }
                        #endregion

                        if (stoppingToken.IsCancellationRequested)
                            break;

                        #region check missed tokens
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            Logger.LogDebug("Load new tokens since #{id}", State.LastTokenId);
                            var tokens = await GetTokens(State.LastTokenId);
                            Logger.LogDebug("{cnt} tokens found", tokens.Count);
                            if (tokens.Count == 0) break;

                            var updates = await FetchTokenMetadata(tokens);
                            Logger.LogDebug("{cnt} updates found", updates.Count);
                            if (updates.Count > 0)
                            {
                                cnt = await SaveTokenMetadata(updates);
                                Logger.LogDebug("{cnt} tokens updated", cnt);
                            }

                            State.LastTokenId = tokens.Values.Max();
                            await SaveState();
                            Logger.LogDebug("State: ({updateId}, {tokenId})", State.LastUpdateId, State.LastTokenId);

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
                Logger.LogError("DipDup metadata crashed: {msg}", ex.Message);
            }
            finally
            {
                Logger.LogInformation("DipDup metadata stopped");
            }
        }

        async Task InitState()
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            var row = await conn.QueryFirstAsync(@"
                SELECT ""Metadata""->'dipDupMetadata' as state, ""TokenCounter""
                FROM ""AppState""
                WHERE ""Id"" = -1
                LIMIT 1");
            
            try { State = JsonSerializer.Deserialize<DipDupMetadataState>(row.state); }
            catch { State = new(); }

            if (State.LastUpdateId == 0)
                State.LastTokenId = row.TokenCounter;
        }

        async Task SaveState()
        {
            var json = JsonSerializer.Serialize(State);
            using var conn = new NpgsqlConnection(ConnectionString);
            await conn.ExecuteAsync($@"
                UPDATE ""AppState""
                SET ""Metadata"" = jsonb_set(COALESCE(""Metadata"", '{{}}'), '{{dipDupMetadata}}', '{json}')");
        }

        async Task<int> InitLegacyTokens()
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            var contracts = await GetContracts(conn, LegacyTokens.Select(x => x.Item1).ToList());
            var tokens = LegacyTokens.Where(x => contracts.ContainsKey(x.Item1));
            return await conn.ExecuteAsync($@"
                UPDATE ""Tokens""
                SET ""Metadata"" = v.metadata FROM (VALUES
                    {string.Join(",", LegacyTokens.Select(x => $"({contracts[x.Item1]}, '0', '{x.Item2}'::jsonb)"))}
                ) as v(contract, token, metadata)
                WHERE ""ContractId"" = v.contract AND ""TokenId"" = v.token AND ""Metadata"" IS NULL");
        }

        static async Task<Dictionary<string, int>> GetContracts(NpgsqlConnection conn, List<string> addresses)
        {
            return (await conn.QueryAsync(@"
                SELECT ""Id"", ""Address""
                FROM ""Accounts""
                WHERE ""Address"" = ANY(@addresses::character(36)[])",
                new { addresses }))
                .ToDictionary(x => (string)x.Address, x => (int)x.Id);
        }

        async Task<Dictionary<(string, string), int>> GetTokens(int lastId)
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

        async Task<List<TokenMetadataItem>> FetchTokenMetadata(int lastUpdateId)
        {
            using var client = new HttpClient();
            using var res = (await client.PostAsync(Config.DipDupUrl, new StringContent(
                $"{{\"query\":\"query{{token_metadata(where:{{network:{{_eq:\\\"{Config.Network}\\\"}},"
                + $"update_id:{{_gt:\\\"{lastUpdateId}\\\"}}}},order_by:{{update_id:asc}},limit:{Config.BatchSize})"
                + $"{{update_id contract token_id metadata}}}}\",\"variables\":null}}",
                Encoding.UTF8, "application/json"))).EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowReadingFromString };
            return (await JsonSerializer.DeserializeAsync<TokenMetadataResponse>(
                await res.Content.ReadAsStreamAsync(), options)).Data.Items;
        }

        async Task<List<TokenMetadataItem>> FetchTokenMetadata(Dictionary<(string, string), int> tokens)
        {
            var contracts = string.Join(',', tokens.Keys.Select(x => $"\\\"{x.Item1}\\\"").Distinct());
            var tokenIds = string.Join(',', tokens.Keys.Select(x => $"\\\"{x.Item2}\\\"").Distinct());
            var items = new List<TokenMetadataItem>(tokens.Count);

            using var client = new HttpClient();
            var options = new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowReadingFromString };
            var lastUpdateId = 0;
            while (true)
            {
                using var res = (await client.PostAsync(Config.DipDupUrl, new StringContent(
                    $"{{\"query\":\"query{{token_metadata(where:{{network:{{_eq:\\\"{Config.Network}\\\"}},"
                    + $"update_id:{{_gt:\\\"{lastUpdateId}\\\"}},contract:{{_in:[{contracts}]}},token_id:{{_in:[{tokenIds}]}}}},"
                    + $"order_by:{{update_id:asc}},limit:{Config.BatchSize}){{update_id contract token_id metadata}}}}\",\"variables\":null}}",
                    Encoding.UTF8, "application/json"))).EnsureSuccessStatusCode();

                var _items = (await JsonSerializer.DeserializeAsync<TokenMetadataResponse>(
                    await res.Content.ReadAsStreamAsync(), options)).Data.Items;
                items.AddRange(_items.Where(x => tokens.ContainsKey((x.Contract, x.TokenId))));
                if (_items.Count < Config.BatchSize) break;
                lastUpdateId = _items[^1].UpdateId;
            }
            return items;
        }

        async Task<int> SaveTokenMetadata(List<TokenMetadataItem> items)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            var contracts = await GetContracts(conn, items.Select(x => x.Contract).ToHashSet().ToList());
            var saved = 0;
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
                        param.Add($"@p{j}", item.Metadata.GetRawText());
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

        class TokenMetadataResponse
        {
            [JsonPropertyName("data")]
            public TokenMetadataQuery Data { get; set; }
        }

        class TokenMetadataQuery
        {
            [JsonPropertyName("token_metadata")]
            public List<TokenMetadataItem> Items { get; set; } = new();
        }

        class TokenMetadataItem
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
