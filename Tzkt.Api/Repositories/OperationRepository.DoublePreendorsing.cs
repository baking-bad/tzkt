using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository
    {
        public async Task<int> GetDoublePreendorsingsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""DoublePreendorsingOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<DoublePreendorsingOperation>> GetDoublePreendorsings(string hash, Symbols quote)
        {
            var sql = """
                SELECT      o.*, b."Hash"
                FROM        "DoublePreendorsingOps" as o
                INNER JOIN  "Blocks" as b 
                        ON  b."Level" = o."Level"
                WHERE       o."OpHash" = @hash::character(51)
                LIMIT       1
                """;

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new DoublePreendorsingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                AccusedLevel = row.AccusedLevel,
                SlashedLevel = row.SlashedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                Reward = row.Reward,
                Offender = Accounts.GetAlias(row.OffenderId),
                LostStaked = row.LostStaked,
                LostUnstaked = row.LostUnstaked,
                LostExternalStaked = row.LostExternalStaked,
                LostExternalUnstaked = row.LostExternalUnstaked,
                StakingUpdatesCount = row.StakingUpdatesCount,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<DoublePreendorsingOperation>> GetDoublePreendorsings(Block block, Symbols quote)
        {
            var sql = """
                SELECT      *
                FROM        "DoublePreendorsingOps"
                WHERE       "Level" = @level
                ORDER BY    "Id"
                """;

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new DoublePreendorsingOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                SlashedLevel = row.SlashedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                Reward = row.Reward,
                Offender = Accounts.GetAlias(row.OffenderId),
                LostStaked = row.LostStaked,
                LostUnstaked = row.LostUnstaked,
                LostExternalStaked = row.LostExternalStaked,
                LostExternalUnstaked = row.LostExternalUnstaked,
                StakingUpdatesCount = row.StakingUpdatesCount,
                Quote = Quotes.Get(quote, block.Level)
            });
        }

        public async Task<IEnumerable<DoublePreendorsingOperation>> GetDoublePreendorsings(
            AnyOfParameter anyof,
            AccountParameter accuser,
            AccountParameter offender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder("""
                SELECT      o.*, b."Hash"
                FROM        "DoublePreendorsingOps" AS o
                INNER JOIN  "Blocks" as b
                        ON  b."Level" = o."Level"
                """)
                .Filter(anyof, x => x == "accuser" ? "AccuserId" : "OffenderId")
                .Filter("AccuserId", accuser, x => "OffenderId")
                .Filter("OffenderId", offender, x => "AccuserId")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "accusedLevel" => ("AccusedLevel", "AccusedLevel"),
                    "slashedLevel" => ("SlashedLevel", "SlashedLevel"),
                    #region deprecated
                    "accuserReward" => ("Reward", "Reward"),
                    "offenderLoss" => ("LostStaked", "LostStaked"),
                    #endregion
                    _ => ("Id", "Id")
                }, "o");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new DoublePreendorsingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                SlashedLevel = row.SlashedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                Reward = row.Reward,
                Offender = Accounts.GetAlias(row.OffenderId),
                LostStaked = row.LostStaked,
                LostUnstaked = row.LostUnstaked,
                LostExternalStaked = row.LostExternalStaked,
                LostExternalUnstaked = row.LostExternalUnstaked,
                StakingUpdatesCount = row.StakingUpdatesCount,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetDoublePreendorsings(
            AnyOfParameter anyof,
            AccountParameter accuser,
            AccountParameter offender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
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
                    case "accusedLevel": columns.Add(@"o.""AccusedLevel"""); break;
                    case "slashedLevel": columns.Add(@"o.""SlashedLevel"""); break;
                    case "accuser": columns.Add(@"o.""AccuserId"""); break;
                    case "reward": columns.Add(@"o.""Reward"""); break;
                    case "offender": columns.Add(@"o.""OffenderId"""); break;
                    case "lostStaked": columns.Add(@"o.""LostStaked"""); break;
                    case "lostUnstaked": columns.Add(@"o.""LostUnstaked"""); break;
                    case "lostExternalStaked": columns.Add(@"o.""LostExternalStaked"""); break;
                    case "lostExternalUnstaked": columns.Add(@"o.""LostExternalUnstaked"""); break;
                    case "stakingUpdatesCount": columns.Add(@"o.""StakingUpdatesCount"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                    #region deprecated
                    case "roundingLoss": columns.Add("0"); break;
                    case "accuserReward":
                        columns.Add(@"o.""Reward""");
                        break;
                    case "offenderLoss":
                        columns.Add(@"o.""LostStaked""");
                        columns.Add(@"o.""LostUnstaked""");
                        columns.Add(@"o.""LostExternalStaked""");
                        columns.Add(@"o.""LostExternalUnstaked""");
                        break;
                    #endregion
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DoublePreendorsingOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x == "accuser" ? "AccuserId" : "OffenderId")
                .Filter("AccuserId", accuser, x => "OffenderId")
                .Filter("OffenderId", offender, x => "AccuserId")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "accusedLevel" => ("AccusedLevel", "AccusedLevel"),
                    "slashedLevel" => ("SlashedLevel", "SlashedLevel"),
                    #region deprecated
                    "accuserReward" => ("Reward", "Reward"),
                    "offenderLoss" => ("LostStaked", "LostStaked"),
                    #endregion
                    _ => ("Id", "Id")
                }, "o");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

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
                    case "accusedLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.AccusedLevel;
                        break;
                    case "slashedLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.SlashedLevel;
                        break;
                    case "accuser":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.AccuserId);
                        break;
                    case "reward":
                        foreach (var row in rows)
                            result[j++][i] = row.Reward;
                        break;
                    case "offender":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.OffenderId);
                        break;
                    case "lostStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.LostStaked;
                        break;
                    case "lostUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.LostUnstaked;
                        break;
                    case "lostExternalStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.LostExternalStaked;
                        break;
                    case "lostExternalUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.LostExternalUnstaked;
                        break;
                    case "stakingUpdatesCount":
                        foreach (var row in rows)
                            result[j++][i] = row.StakingUpdatesCount;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                    #region deprecated
                    case "roundingLoss":
                        foreach (var row in rows)
                            result[j++][i] = 0;
                        break;
                    case "accuserReward":
                        foreach (var row in rows)
                            result[j++][i] = row.Reward;
                        break;
                    case "offenderLoss":
                        foreach (var row in rows)
                            result[j++][i] = row.LostStaked + row.LostUnstaked + row.LostExternalStaked + row.LostExternalUnstaked;
                        break;
                    #endregion
                }
            }

            return result;
        }

        public async Task<object[]> GetDoublePreendorsings(
            AnyOfParameter anyof,
            AccountParameter accuser,
            AccountParameter offender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
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
                case "accusedLevel": columns.Add(@"o.""AccusedLevel"""); break;
                case "slashedLevel": columns.Add(@"o.""SlashedLevel"""); break;
                case "accuser": columns.Add(@"o.""AccuserId"""); break;
                case "reward": columns.Add(@"o.""Reward"""); break;
                case "offender": columns.Add(@"o.""OffenderId"""); break;
                case "lostStaked": columns.Add(@"o.""LostStaked"""); break;
                case "lostUnstaked": columns.Add(@"o.""LostUnstaked"""); break;
                case "lostExternalStaked": columns.Add(@"o.""LostExternalStaked"""); break;
                case "lostExternalUnstaked": columns.Add(@"o.""LostExternalUnstaked"""); break;
                case "stakingUpdatesCount": columns.Add(@"o.""StakingUpdatesCount"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
                #region deprecated
                case "roundingLoss": columns.Add("0"); break;
                case "accuserReward":
                    columns.Add(@"o.""Reward""");
                    break;
                case "offenderLoss":
                    columns.Add(@"o.""LostStaked""");
                    columns.Add(@"o.""LostUnstaked""");
                    columns.Add(@"o.""LostExternalStaked""");
                    columns.Add(@"o.""LostExternalUnstaked""");
                    break;
                #endregion
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DoublePreendorsingOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x == "accuser" ? "AccuserId" : "OffenderId")
                .Filter("AccuserId", accuser, x => "OffenderId")
                .Filter("OffenderId", offender, x => "AccuserId")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "accusedLevel" => ("AccusedLevel", "AccusedLevel"),
                    "slashedLevel" => ("SlashedLevel", "SlashedLevel"),
                    #region deprecated
                    "accuserReward" => ("Reward", "Reward"),
                    "offenderLoss" => ("LostStaked", "LostStaked"),
                    #endregion
                    _ => ("Id", "Id")
                }, "o");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
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
                case "accusedLevel":
                    foreach (var row in rows)
                        result[j++] = row.AccusedLevel;
                    break;
                case "slashedLevel":
                    foreach (var row in rows)
                        result[j++] = row.SlashedLevel;
                    break;
                case "accuser":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.AccuserId);
                    break;
                case "reward":
                    foreach (var row in rows)
                        result[j++] = row.Reward;
                    break;
                case "offender":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.OffenderId);
                    break;
                case "lostStaked":
                    foreach (var row in rows)
                        result[j++] = row.LostStaked;
                    break;
                case "lostUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.LostUnstaked;
                    break;
                case "lostExternalStaked":
                    foreach (var row in rows)
                        result[j++] = row.LostExternalStaked;
                    break;
                case "lostExternalUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.LostExternalUnstaked;
                    break;
                case "stakingUpdatesCount":
                    foreach (var row in rows)
                        result[j++] = row.StakingUpdatesCount;
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
                #region deprecated
                case "roundingLoss":
                    foreach (var row in rows)
                        result[j++] = 0;
                    break;
                case "accuserReward":
                    foreach (var row in rows)
                        result[j++] = row.Reward;
                    break;
                case "offenderLoss":
                    foreach (var row in rows)
                        result[j++] = row.LostStaked + row.LostUnstaked + row.LostExternalStaked + row.LostExternalUnstaked;
                    break;
                #endregion
            }

            return result;
        }
    }
}
