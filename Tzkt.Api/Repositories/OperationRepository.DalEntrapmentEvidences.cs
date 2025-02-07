using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository
    {
        public async Task<int> GetDalEntrapmentEvidencesCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""DalEntrapmentEvidenceOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<DalEntrapmentEvidenceOperation>> GetDalEntrapmentEvidences(string hash, Symbols quote)
        {
            var sql = """
                SELECT      o.*, b."Hash"
                FROM        "DalEntrapmentEvidenceOps" as o
                INNER JOIN  "Blocks" as b 
                        ON  b."Level" = o."Level"
                WHERE       o."OpHash" = @hash::character(51)
                LIMIT       1
                """;

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new DalEntrapmentEvidenceOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Accuser = Accounts.GetAlias(row.AccuserId),
                Offender = Accounts.GetAlias(row.OffenderId),
                TrapLevel = row.TrapLevel,
                TrapSlotIndex= row.TrapSlotIndex,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<DalEntrapmentEvidenceOperation>> GetDalEntrapmentEvidences(Block block, Symbols quote)
        {
            var sql = """
                SELECT      *
                FROM        "DalEntrapmentEvidenceOps"
                WHERE       "Level" = @level
                ORDER BY    "Id"
                """;

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new DalEntrapmentEvidenceOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Accuser = Accounts.GetAlias(row.AccuserId),
                Offender = Accounts.GetAlias(row.OffenderId),
                TrapLevel = row.TrapLevel,
                TrapSlotIndex = row.TrapSlotIndex,
                Quote = Quotes.Get(quote, block.Level)
            });
        }

        public async Task<IEnumerable<DalEntrapmentEvidenceOperation>> GetDalEntrapmentEvidences(
            AnyOfParameter anyof,
            AccountParameter accuser,
            AccountParameter offender,
            Int64Parameter id,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder("""
                SELECT      o.*, b."Hash"
                FROM        "DalEntrapmentEvidenceOps" AS o
                INNER JOIN  "Blocks" as b
                        ON  b."Level" = o."Level"
                """)
                .Filter(anyof, x => x == "accuser" ? "AccuserId" : "OffenderId")
                .Filter("AccuserId", accuser, x => "OffenderId")
                .Filter("OffenderId", offender, x => "AccuserId")
                .FilterA(@"o.""Id""", id)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "trapLevel" => ("TrapLevel", "TrapLevel"),
                    _ => ("Id", "Id")
                }, "o");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new DalEntrapmentEvidenceOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Accuser = Accounts.GetAlias(row.AccuserId),
                Offender = Accounts.GetAlias(row.OffenderId),
                TrapLevel = row.TrapLevel,
                TrapSlotIndex = row.TrapSlotIndex,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetDalEntrapmentEvidences(
            AnyOfParameter anyof,
            AccountParameter accuser,
            AccountParameter offender,
            Int64Parameter id,
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
                    case "accuser": columns.Add(@"o.""AccuserId"""); break;
                    case "offender": columns.Add(@"o.""OffenderId"""); break;
                    case "trapLevel": columns.Add(@"o.""TrapLevel"""); break;
                    case "trapSlotIndex": columns.Add(@"o.""TrapSlotIndex"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DalEntrapmentEvidenceOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x == "accuser" ? "AccuserId" : "OffenderId")
                .Filter("AccuserId", accuser, x => "OffenderId")
                .Filter("OffenderId", offender, x => "AccuserId")
                .FilterA(@"o.""Id""", id)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "trapLevel" => ("TrapLevel", "TrapLevel"),
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
                    case "accuser":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.AccuserId);
                        break;
                    case "offender":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.OffenderId);
                        break;
                    case "trapLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.TrapLevel;
                        break;
                    case "trapSlotIndex":
                        foreach (var row in rows)
                            result[j++][i] = row.TrapSlotIndex;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetDalEntrapmentEvidences(
            AnyOfParameter anyof,
            AccountParameter accuser,
            AccountParameter offender,
            Int64Parameter id,
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
                case "accuser": columns.Add(@"o.""AccuserId"""); break;
                case "offender": columns.Add(@"o.""OffenderId"""); break;
                case "trapLevel": columns.Add(@"o.""TrapLevel"""); break;
                case "trapSlotIndex": columns.Add(@"o.""TrapSlotIndex"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DalEntrapmentEvidenceOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x == "accuser" ? "AccuserId" : "OffenderId")
                .Filter("AccuserId", accuser, x => "OffenderId")
                .Filter("OffenderId", offender, x => "AccuserId")
                .FilterA(@"o.""Id""", id)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "trapLevel" => ("TrapLevel", "TrapLevel"),
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
                case "accuser":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.AccuserId);
                    break;
                case "offender":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.OffenderId);
                    break;
                case "trapLevel":
                    foreach (var row in rows)
                        result[j++] = row.TrapLevel;
                    break;
                case "trapSlotIndex":
                    foreach (var row in rows)
                        result[j++] = row.TrapSlotIndex;
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
