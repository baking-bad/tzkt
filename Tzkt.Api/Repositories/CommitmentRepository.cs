using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class CommitmentRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly TimeCache Time;

        public CommitmentRepository(AccountsCache accounts, TimeCache time, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Time = time;
        }

        public async Task<int> GetCount(bool? activated, Int64Parameter balance)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""Commitments""")
                .Filter("Level", activated == null ? null : new Int32NullParameter { Null = !activated })
                .Filter("Balance", balance);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<Commitment> Get(string address)
        {
            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(@"SELECT * FROM ""Commitments"" WHERE ""Address"" = @address::character(37)", new { address });
            if (row == null) return null;

            return new Commitment
            {
                Activated = row.Level != null,
                ActivatedAccount = row.AccountId == null ? null : Accounts.GetAlias(row.AccountId),
                ActivationLevel = row.Level,
                ActivationTime = row.Level == null ? null : Time[row.Level],
                Address = row.Address,
                Balance = row.Balance
            };
        }

        public async Task<IEnumerable<Commitment>> Get(
            bool? activated,
            Int32NullParameter activationLevel,
            Int64Parameter balance,
            SortParameter sort,
            OffsetParameter offset,
            int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""Commitments""")
                .Filter("Level", activated == null ? null : new Int32NullParameter { Null = !activated })
                .Filter("Level", activationLevel)
                .Filter("Balance", balance)
                .Take(sort, offset, limit, x => x switch
                {
                    "balance" => ("Balance", "Balance"),
                    "activationLevel" => ("Level", "Level"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new Commitment
            {
                Activated = row.Level != null,
                ActivatedAccount = row.AccountId == null ? null : Accounts.GetAlias(row.AccountId),
                ActivationLevel = row.Level,
                ActivationTime = row.Level == null ? null : Time[row.Level],
                Address = row.Address,
                Balance = row.Balance
            });
        }

        public async Task<object[][]> Get(
            bool? activated,
            Int32NullParameter activationLevel,
            Int64Parameter balance,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "activated": columns.Add(@"""Level"""); break;
                    case "activatedAccount": columns.Add(@"""AccountId"""); break;
                    case "activationLevel": columns.Add(@"""Level"""); break;
                    case "activationTime": columns.Add(@"""Level"""); break;
                    case "address": columns.Add(@"""Address"""); break;
                    case "balance": columns.Add(@"""Balance"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Commitments""")
                .Filter("Level", activated == null ? null : new Int32NullParameter { Null = !activated })
                .Filter("Level", activationLevel)
                .Filter("Balance", balance)
                .Take(sort, offset, limit, x => x switch
                {
                    "balance" => ("Balance", "Balance"),
                    "activationLevel" => ("Level", "Level"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "activated":
                        foreach (var row in rows)
                            result[j++][i] = row.Level != null;
                        break;
                    case "activatedAccount":
                        foreach (var row in rows)
                            result[j++][i] = row.AccountId == null ? null : Accounts.GetAlias(row.AccountId);
                        break;
                    case "activationLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "activationTime":
                        foreach (var row in rows)
                            result[j++][i] = row.Level == null ? null : Time[row.Level];
                        break;
                    case "address":
                        foreach (var row in rows)
                            result[j++][i] = row.Address;
                        break;
                    case "balance":
                        foreach (var row in rows)
                            result[j++][i] = row.Balance;
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> Get(
            bool? activated,
            Int32NullParameter activationLevel,
            Int64Parameter balance,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field)
        {
            var columns = new HashSet<string>(2);
            switch (field)
            {
                case "activated": columns.Add(@"""Level"""); break;
                case "activatedAccount": columns.Add(@"""AccountId"""); break;
                case "activationLevel": columns.Add(@"""Level"""); break;
                case "activationTime": columns.Add(@"""Level"""); break;
                case "address": columns.Add(@"""Address"""); break;
                case "balance": columns.Add(@"""Balance"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Commitments""")
                .Filter("Level", activated == null ? null : new Int32NullParameter { Null = !activated })
                .Filter("Level", activationLevel)
                .Filter("Balance", balance)
                .Take(sort, offset, limit, x => x switch
                {
                    "balance" => ("Balance", "Balance"),
                    "activationLevel" => ("Level", "Level"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "activated":
                    foreach (var row in rows)
                        result[j++] = row.Level != null;
                    break;
                case "activatedAccount":
                    foreach (var row in rows)
                        result[j++] = row.AccountId == null ? null : Accounts.GetAlias(row.AccountId);
                    break;
                case "activationLevel":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "activationTime":
                    foreach (var row in rows)
                        result[j++] = row.Level == null ? null : Time[row.Level];
                    break;
                case "address":
                    foreach (var row in rows)
                        result[j++] = row.Address;
                    break;
                case "balance":
                    foreach (var row in rows)
                        result[j++] = row.Balance;
                    break;
            }

            return result;
        }
    }
}
