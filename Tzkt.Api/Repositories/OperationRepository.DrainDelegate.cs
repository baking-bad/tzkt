using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository : DbConnection
    {
        public async Task<int> GetDrainDelegatesCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""DrainDelegateOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<DrainDelegateOperation>> GetDrainDelegates(string hash, Symbols quote)
        {
            var sql = @"
                SELECT      o.*, b.""Hash""
                FROM        ""DrainDelegateOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new DrainDelegateOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Delegate = Accounts.GetAlias(row.DelegateId),
                Target = Accounts.GetAlias(row.TargetId),
                Amount = row.Amount,
                Fee = row.Fee,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<DrainDelegateOperation>> GetDrainDelegates(Block block, Symbols quote)
        {
            var sql = @"
                SELECT    *
                FROM      ""DrainDelegateOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new DrainDelegateOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Accounts.GetAlias(row.DelegateId),
                Target = Accounts.GetAlias(row.TargetId),
                Amount = row.Amount,
                Fee = row.Fee,
                Quote = Quotes.Get(quote, block.Level)
            });
        }

        public async Task<IEnumerable<DrainDelegateOperation>> GetDrainDelegates(
            AnyOfParameter anyof,
            AccountParameter @delegate,
            AccountParameter target,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""DrainDelegateOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .FilterA(anyof, x => x == "delegate" ? @"o.""DelegateId""" : @"o.""TargetId""")
                .FilterA(@"o.""DelegateId""", @delegate)
                .FilterA(@"o.""TargetId""", target)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new DrainDelegateOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Accounts.GetAlias(row.DelegateId),
                Target = Accounts.GetAlias(row.TargetId),
                Amount = row.Amount,
                Fee = row.Fee,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetDrainDelegates(
            AnyOfParameter anyof,
            AccountParameter @delegate,
            AccountParameter target,
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
                    case "delegate": columns.Add(@"o.""DelegateId"""); break;
                    case "target": columns.Add(@"o.""TargetId"""); break;
                    case "amount": columns.Add(@"o.""Amount"""); break;
                    case "fee": columns.Add(@"o.""Fee"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DrainDelegateOps"" as o {string.Join(' ', joins)}")
                .FilterA(anyof, x => x == "delegate" ? @"o.""DelegateId""" : @"o.""TargetId""")
                .FilterA(@"o.""DelegateId""", @delegate)
                .FilterA(@"o.""TargetId""", target)
                .FilterA(@"o.""Level""", level)
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
                    case "delegate":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.DelegateId);
                        break;
                    case "target":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.TargetId);
                        break;
                    case "amount":
                        foreach (var row in rows)
                            result[j++][i] = row.Amount;
                        break;
                    case "fee":
                        foreach (var row in rows)
                            result[j++][i] = row.Fee;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetDrainDelegates(
            AnyOfParameter anyof,
            AccountParameter @delegate,
            AccountParameter target,
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
                case "delegate": columns.Add(@"o.""DelegateId"""); break;
                case "target": columns.Add(@"o.""TargetId"""); break;
                case "amount": columns.Add(@"o.""Amount"""); break;
                case "fee": columns.Add(@"o.""Fee"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DrainDelegateOps"" as o {string.Join(' ', joins)}")
                .FilterA(anyof, x => x == "delegate" ? @"o.""DelegateId""" : @"o.""TargetId""")
                .FilterA(@"o.""DelegateId""", @delegate)
                .FilterA(@"o.""TargetId""", target)
                .FilterA(@"o.""Level""", level)
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
                case "delegate":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.DelegateId);
                    break;
                case "target":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.TargetId);
                    break;
                case "amount":
                    foreach (var row in rows)
                        result[j++] = row.Amount;
                    break;
                case "fee":
                    foreach (var row in rows)
                        result[j++] = row.Fee;
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
