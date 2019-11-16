using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services;

namespace Tzkt.Api.Repositories
{
    public class AccountRepository : DbConnection
    {
        readonly AliasService Aliases;

        public AccountRepository(AliasService aliases, IConfiguration config) : base(config)
        {
            Aliases = aliases;
        }

        public async Task<Account> Get(string address)
        {
            var sql = @"
                SELECT  ""Id"", ""Type"", ""FirstLevel"", ""LastLevel"", ""Balance"", ""DelegateId"", ""Staked""
                FROM    ""Accounts""
                WHERE   ""Address"" = @address
                LIMIT   1";

            using var db = GetConnection();
            var item = await db.QueryFirstOrDefaultAsync(sql, new { address });
            if (item == null) return null;

            var type = (int)item.Type;
            var delegateId = (int?)item.DelegateId;
            return new Account
            {
                Type = type switch
                {
                    0 => "user",
                    1 => "delegate",
                    2 => "contract",
                    _ => "unknown"
                },
                Alias = Aliases[item.Id].Name,
                Address = address,
                Balance = item.Balance,
                Delegate = delegateId == null ? null : new DelegateInfo(Aliases[(int)delegateId], item.Staked),
                FirstActivity = item.FirstLevel,
                LastActivity = item.LastLevel
            };
        }

        public async Task<IEnumerable<Account>> Get(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT   ""Id"", ""Type"", ""Address"", ""FirstLevel"", ""LastLevel"", ""Balance"", ""DelegateId"", ""Staked""
                FROM     ""Accounts""
                ORDER BY ""Id""
                OFFSET   @offset
                LIMIT    @limit";

            using var db = GetConnection();
            var items = await db.QueryAsync(sql, new { limit, offset });

            var accounts = new List<Account>(items.Count());
            foreach (var item in items)
            {
                var type = (int)item.Type;
                var delegateId = (int?)item.DelegateId;
                accounts.Add(new Account
                {
                    Type = type switch
                    {
                        0 => "user",
                        1 => "delegate",
                        2 => "contract",
                        _ => "unknown"
                    },
                    Alias = Aliases[item.Id].Name,
                    Address = item.Address,
                    Balance = item.Balance,
                    Delegate = delegateId == null ? null : new DelegateInfo(Aliases[(int)delegateId], item.Staked),
                    FirstActivity = item.FirstLevel,
                    LastActivity = item.LastLevel
                });
            }

            return accounts;
        }
    }
}
