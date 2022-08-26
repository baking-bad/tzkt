using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Netezos.Encoding;
using Tzkt.Api.Models;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository : DbConnection
    {
        public async Task<int> GetVdfRevelationsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""VdfRevelationOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<VdfRevelationOperation>> GetVdfRevelations(string hash, Symbols quote)
        {
            var sql = @"
                SELECT      o.*, b.""Hash""
                FROM        ""VdfRevelationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new VdfRevelationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Baker = Accounts.GetAlias(row.BakerId),
                Cycle = row.Cycle,
                Solution = Hex.Convert(row.Solution),
                Proof = Hex.Convert(row.Proof),
                Reward = row.Reward,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<VdfRevelationOperation>> GetVdfRevelations(Block block, Symbols quote)
        {
            var sql = @"
                SELECT    *
                FROM      ""VdfRevelationOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new VdfRevelationOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Baker = Accounts.GetAlias(row.BakerId),
                Cycle = row.Cycle,
                Solution = Hex.Convert(row.Solution),
                Proof = Hex.Convert(row.Proof),
                Reward = row.Reward,
                Quote = Quotes.Get(quote, block.Level)
            });
        }

        public async Task<IEnumerable<VdfRevelationOperation>> GetVdfRevelations(
            AccountParameter baker,
            Int32Parameter level,
            Int32Parameter cycle,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""VdfRevelationOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .FilterA(@"o.""BakerId""", baker)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Cycle""", cycle)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new VdfRevelationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Baker = Accounts.GetAlias(row.BakerId),
                Cycle = row.Cycle,
                Solution = Hex.Convert(row.Solution),
                Proof = Hex.Convert(row.Proof),
                Reward = row.Reward,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetVdfRevelations(
            AccountParameter baker,
            Int32Parameter level,
            Int32Parameter cycle,
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
                    case "baker": columns.Add(@"o.""BakerId"""); break;
                    case "cycle": columns.Add(@"o.""Cycle"""); break;
                    case "solution": columns.Add(@"o.""Solution"""); break;
                    case "proof": columns.Add(@"o.""Proof"""); break;
                    case "reward": columns.Add(@"o.""Reward"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""VdfRevelationOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""BakerId""", baker)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Cycle""", cycle)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
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
                    case "baker":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.BakerId);
                        break;
                    case "cycle":
                        foreach (var row in rows)
                            result[j++][i] = row.Cycle;
                        break;
                    case "solution":
                        foreach (var row in rows)
                            result[j++][i] = Hex.Convert(row.Solution);
                        break;
                    case "proof":
                        foreach (var row in rows)
                            result[j++][i] = Hex.Convert(row.Proof);
                        break;
                    case "reward":
                        foreach (var row in rows)
                            result[j++][i] = row.Reward;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetVdfRevelations(
            AccountParameter baker,
            Int32Parameter level,
            Int32Parameter cycle,
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
                case "baker": columns.Add(@"o.""BakerId"""); break;
                case "cycle": columns.Add(@"o.""Cycle"""); break;
                case "solution": columns.Add(@"o.""Solution"""); break;
                case "proof": columns.Add(@"o.""Proof"""); break;
                case "reward": columns.Add(@"o.""Reward"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""VdfRevelationOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""BakerId""", baker)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Cycle""", cycle)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
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
                case "baker":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.BakerId);
                    break;
                case "cycle":
                    foreach (var row in rows)
                        result[j++] = row.Cycle;
                    break;
                case "solution":
                    foreach (var row in rows)
                        result[j++] = Hex.Convert(row.Solution);
                    break;
                case "proof":
                    foreach (var row in rows)
                        result[j++] = Hex.Convert(row.Proof);
                    break;
                case "reward":
                    foreach (var row in rows)
                        result[j++] = row.Reward;
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
