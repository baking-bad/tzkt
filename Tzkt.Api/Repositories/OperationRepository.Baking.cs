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
        public async Task<int> GetBakingsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""Blocks""")
                .Filter(@"""ProducerId"" IS NOT NULL")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<BakingOperation> GetBaking(long id, Symbols quote)
        {
            var sql = $@"
                SELECT      *
                FROM        ""Blocks""
                WHERE       ""Id"" = @id
                LIMIT       1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { id });
            if (row == null) return null;

            return new BakingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Proposer = Accounts.GetAlias(row.ProposerId),
                Producer = Accounts.GetAlias(row.ProducerId),
                PayloadRound = row.PayloadRound,
                BlockRound = row.BlockRound,
                Block = row.Hash,
                Deposit = row.Deposit,
                Reward = row.Reward,
                Bonus = row.Bonus,
                Fees = row.Fees,
                Quote = Quotes.Get(quote, row.Level)
            };
        }

        public async Task<IEnumerable<BakingOperation>> GetBakings(
            AnyOfParameter anyof,
            AccountParameter proposer,
            AccountParameter producer,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""Blocks""")
                .Filter(anyof, x => x == "proposer" ? "ProposerId" : "ProducerId")
                .Filter("ProposerId", proposer)
                .Filter("ProducerId", producer)
                .Filter(@"""ProducerId"" IS NOT NULL")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new BakingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Proposer = Accounts.GetAlias(row.ProposerId),
                Producer = Accounts.GetAlias(row.ProducerId),
                PayloadRound = row.PayloadRound,
                BlockRound = row.BlockRound,
                Block = row.Hash,
                Deposit = row.Deposit,
                Reward = row.Reward,
                Bonus = row.Bonus,
                Fees = row.Fees,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetBakings(
            AnyOfParameter anyof,
            AccountParameter proposer,
            AccountParameter producer,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"""Id"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "block": columns.Add(@"""Hash"""); break;
                    case "proposer": columns.Add(@"""ProposerId"""); break;
                    case "producer": columns.Add(@"""ProducerId"""); break;
                    case "payloadRound": columns.Add(@"""PayloadRound"""); break;
                    case "blockRound": columns.Add(@"""BlockRound"""); break;
                    case "deposit": columns.Add(@"""Deposit"""); break;
                    case "reward": columns.Add(@"""Reward"""); break;
                    case "bonus": columns.Add(@"""Bonus"""); break;
                    case "fees": columns.Add(@"""Fees"""); break;
                    case "quote": columns.Add(@"""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Filter(anyof, x => x == "proposer" ? "ProposerId" : "ProducerId")
                .Filter("ProposerId", proposer)
                .Filter("ProducerId", producer)
                .Filter(@"""ProducerId"" IS NOT NULL")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"));

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
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
                        break;
                    case "proposer":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.ProposerId);
                        break;
                    case "producer":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.ProducerId);
                        break;
                    case "payloadRound":
                        foreach (var row in rows)
                            result[j++][i] = row.PayloadRound;
                        break;
                    case "blockRound":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRound;
                        break;
                    case "deposit":
                        foreach (var row in rows)
                            result[j++][i] = row.Deposit;
                        break;
                    case "reward":
                        foreach (var row in rows)
                            result[j++][i] = row.Reward;
                        break;
                    case "bonus":
                        foreach (var row in rows)
                            result[j++][i] = row.Bonus;
                        break;
                    case "fees":
                        foreach (var row in rows)
                            result[j++][i] = row.Fees;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetBakings(
            AnyOfParameter anyof,
            AccountParameter proposer,
            AccountParameter producer,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "id": columns.Add(@"""Id"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "block": columns.Add(@"""Hash"""); break;
                case "proposer": columns.Add(@"""ProposerId"""); break;
                case "producer": columns.Add(@"""ProducerId"""); break;
                case "payloadRound": columns.Add(@"""PayloadRound"""); break;
                case "blockRound": columns.Add(@"""BlockRound"""); break;
                case "deposit": columns.Add(@"""Deposit"""); break;
                case "reward": columns.Add(@"""Reward"""); break;
                case "bonus": columns.Add(@"""Bonus"""); break;
                case "fees": columns.Add(@"""Fees"""); break;
                case "quote": columns.Add(@"""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Filter(anyof, x => x == "proposer" ? "ProposerId" : "ProducerId")
                .Filter("ProposerId", proposer)
                .Filter("ProducerId", producer)
                .Filter(@"""ProducerId"" IS NOT NULL")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"));

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
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
                    break;
                case "proposer":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.ProposerId);
                    break;
                case "producer":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.ProducerId);
                    break;
                case "payloadRound":
                    foreach (var row in rows)
                        result[j++] = row.PayloadRound;
                    break;
                case "blockRound":
                    foreach (var row in rows)
                        result[j++] = row.BlockRound;
                    break;
                case "deposit":
                    foreach (var row in rows)
                        result[j++] = row.Deposit;
                    break;
                case "reward":
                    foreach (var row in rows)
                        result[j++] = row.Reward;
                    break;
                case "bonus":
                    foreach (var row in rows)
                        result[j++] = row.Bonus;
                    break;
                case "fees":
                    foreach (var row in rows)
                        result[j++] = row.Fees;
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
