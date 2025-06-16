using Dapper;
using Netezos.Encoding;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository
    {
        public async Task<int> GetNonceRevelationsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""NonceRevelationOps""")
                .Filter("Level", level)
                .Filter("Level", timestamp);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(string hash, Symbols quote)
        {
            var sql = @"
                SELECT      o.*, b.""Hash""
                FROM        ""NonceRevelationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new NonceRevelationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Baker = Accounts.GetAlias(row.BakerId),
                Sender = Accounts.GetAlias(row.SenderId),
                RevealedLevel = row.RevealedLevel,
                RevealedCycle = row.RevealedCycle,
                Nonce = Hex.Convert(row.Nonce),
                RewardDelegated = row.RewardDelegated,
                RewardStakedOwn = row.RewardStakedOwn,
                RewardStakedEdge = row.RewardStakedEdge,
                RewardStakedShared = row.RewardStakedShared,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(Block block, Symbols quote)
        {
            var sql = @"
                SELECT    *
                FROM      ""NonceRevelationOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new NonceRevelationOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Baker = Accounts.GetAlias(row.BakerId),
                Sender = Accounts.GetAlias(row.SenderId),
                RevealedLevel = row.RevealedLevel,
                RevealedCycle = row.RevealedCycle,
                Nonce = Hex.Convert(row.Nonce),
                RewardDelegated = row.RewardDelegated,
                RewardStakedOwn = row.RewardStakedOwn,
                RewardStakedEdge = row.RewardStakedEdge,
                RewardStakedShared = row.RewardStakedShared,
                Quote = Quotes.Get(quote, block.Level)
            });
        }
        public async Task<IEnumerable<Activity>> GetNonceRevelationOpsActivity(
            List<RawAccount> accounts,
            ActivityRole roles,
            TimestampParameter? timestamp,
            Pagination pagination,
            Symbols quote)
        {
            List<int>? senderIds = null;
            List<int>? bakerIds = null;

            foreach (var account in accounts)
            {
                if (account is not RawDelegate baker || baker.NonceRevelationsCount == 0)
                    continue;

                if ((roles & ActivityRole.Sender) != 0)
                {
                    senderIds ??= new(accounts.Count);
                    senderIds.Add(account.Id);
                }

                if ((roles & ActivityRole.Mention) != 0)
                {
                    bakerIds ??= new(accounts.Count);
                    bakerIds.Add(account.Id);
                }
            }

            if (senderIds == null && bakerIds == null)
                return [];

            var or = new OrParameter(
                (@"o.""SenderId""", senderIds),
                (@"o.""BakerId""", bakerIds));

            return await GetNonceRevelations(
                or,
                null, null, null, null, null,
                timestamp,
                pagination.sort,
                pagination.offset,
                pagination.limit,
                quote);
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(
            OrParameter? or,
            AnyOfParameter? anyof,
            AccountParameter? baker,
            AccountParameter? sender,
            Int32Parameter? level,
            Int32Parameter? revealedCycle,
            TimestampParameter? timestamp,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""NonceRevelationOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .FilterA(or)
                .FilterA(anyof, x => x == "baker" ? @"o.""BakerId""" : @"o.""SenderId""")
                .FilterA(@"o.""BakerId""", baker, x => @"o.""SenderId""")
                .FilterA(@"o.""SenderId""", sender, x => @"o.""BakerId""")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""RevealedCycle""", revealedCycle)
                .FilterA(@"o.""Level""", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "revealedLevel" => ("RevealedLevel", "RevealedLevel"),
                    _ => ("Id", "Id")
                }, "o");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new NonceRevelationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Baker = Accounts.GetAlias(row.BakerId),
                Sender = Accounts.GetAlias(row.SenderId),
                RevealedLevel = row.RevealedLevel,
                RevealedCycle = row.RevealedCycle,
                Nonce = Hex.Convert(row.Nonce),
                RewardDelegated = row.RewardDelegated,
                RewardStakedOwn = row.RewardStakedOwn,
                RewardStakedEdge = row.RewardStakedEdge,
                RewardStakedShared = row.RewardStakedShared,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object?[][]> GetNonceRevelations(
            AnyOfParameter? anyof,
            AccountParameter? baker,
            AccountParameter? sender,
            Int32Parameter? level,
            Int32Parameter? revealedCycle,
            TimestampParameter? timestamp,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            string[] fields,
            Symbols quote)
        {
            var columns = new HashSet<string>(fields.Length);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "hash": columns.Add(@"o.""OpHash"""); break;
                    case "baker": columns.Add(@"o.""BakerId"""); break;
                    case "sender": columns.Add(@"o.""SenderId"""); break;
                    case "revealedLevel": columns.Add(@"o.""RevealedLevel"""); break;
                    case "revealedCycle": columns.Add(@"o.""RevealedCycle"""); break;
                    case "nonce": columns.Add(@"o.""Nonce"""); break;
                    case "rewardDelegated": columns.Add(@"o.""RewardDelegated"""); break;
                    case "rewardStakedOwn": columns.Add(@"o.""RewardStakedOwn"""); break;
                    case "rewardStakedEdge": columns.Add(@"o.""RewardStakedEdge"""); break;
                    case "rewardStakedShared": columns.Add(@"o.""RewardStakedShared"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""NonceRevelationOps"" as o {string.Join(' ', joins)}")
                .FilterA(anyof, x => x == "baker" ? @"o.""BakerId""" : @"o.""SenderId""")
                .FilterA(@"o.""BakerId""", baker, x => @"o.""SenderId""")
                .FilterA(@"o.""SenderId""", sender, x => @"o.""BakerId""")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""RevealedCycle""", revealedCycle)
                .FilterA(@"o.""Level""", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "revealedLevel" => ("RevealedLevel", "RevealedLevel"),
                    _ => ("Id", "Id")
                }, "o");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "baker":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.BakerId);
                        break;
                    case "sender":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.SenderId);
                        break;
                    case "revealedLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.RevealedLevel;
                        break;
                    case "revealedCycle":
                        foreach (var row in rows)
                            result[j++][i] = row.RevealedCycle;
                        break;
                    case "nonce":
                        foreach (var row in rows)
                            result[j++][i] = Hex.Convert(row.Nonce);
                        break;
                    case "rewardDelegated":
                        foreach (var row in rows)
                            result[j++][i] = row.RewardDelegated;
                        break;
                    case "rewardStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.RewardStakedOwn;
                        break;
                    case "rewardStakedEdge":
                        foreach (var row in rows)
                            result[j++][i] = row.RewardStakedEdge;
                        break;
                    case "rewardStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.RewardStakedShared;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object?[]> GetNonceRevelations(
            AnyOfParameter? anyof,
            AccountParameter? baker,
            AccountParameter? sender,
            Int32Parameter? level,
            Int32Parameter? revealedCycle,
            TimestampParameter? timestamp,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            string field,
            Symbols quote)
        {
            var columns = new HashSet<string>(1);
            var joins = new HashSet<string>(1);

            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "hash": columns.Add(@"o.""OpHash"""); break;
                case "baker": columns.Add(@"o.""BakerId"""); break;
                case "sender": columns.Add(@"o.""SenderId"""); break;
                case "revealedLevel": columns.Add(@"o.""RevealedLevel"""); break;
                case "revealedCycle": columns.Add(@"o.""RevealedCycle"""); break;
                case "nonce": columns.Add(@"o.""Nonce"""); break;
                case "rewardDelegated": columns.Add(@"o.""RewardDelegated"""); break;
                case "rewardStakedOwn": columns.Add(@"o.""RewardStakedOwn"""); break;
                case "rewardStakedEdge": columns.Add(@"o.""RewardStakedEdge"""); break;
                case "rewardStakedShared": columns.Add(@"o.""RewardStakedShared"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""NonceRevelationOps"" as o {string.Join(' ', joins)}")
                .FilterA(anyof, x => x == "baker" ? @"o.""BakerId""" : @"o.""SenderId""")
                .FilterA(@"o.""BakerId""", baker, x => @"o.""SenderId""")
                .FilterA(@"o.""SenderId""", sender, x => @"o.""BakerId""")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""RevealedCycle""", revealedCycle)
                .FilterA(@"o.""Level""", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "revealedLevel" => ("RevealedLevel", "RevealedLevel"),
                    _ => ("Id", "Id")
                }, "o");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object?[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.OpHash;
                    break;
                case "baker":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.BakerId);
                    break;
                case "sender":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.SenderId);
                    break;
                case "revealedLevel":
                    foreach (var row in rows)
                        result[j++] = row.RevealedLevel;
                    break;
                case "revealedCycle":
                    foreach (var row in rows)
                        result[j++] = row.RevealedCycle;
                    break;
                case "nonce":
                    foreach (var row in rows)
                        result[j++] = Hex.Convert(row.Nonce);
                    break;
                case "rewardDelegated":
                    foreach (var row in rows)
                        result[j++] = row.RewardDelegated;
                    break;
                case "rewardStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.RewardStakedOwn;
                    break;
                case "rewardStakedEdge":
                    foreach (var row in rows)
                        result[j++] = row.RewardStakedEdge;
                    break;
                case "rewardStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.RewardStakedShared;
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
    }
}
