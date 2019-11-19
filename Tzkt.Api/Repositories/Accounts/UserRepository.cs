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
    public class UserRepository : DbConnection
    {
        readonly AliasService Aliases;
        readonly StateService State;

        public UserRepository(AliasService aliases, StateService state, IConfiguration config) : base(config)
        {
            Aliases = aliases;
            State = state;
        }

        public async Task<User> Get(string address)
        {
            var sql = @"
                SELECT  ""Id"", ""FirstLevel"", ""LastLevel"", ""Balance"", ""Counter"", ""DelegateId"", ""Staked"", ""PublicKey""
                FROM    ""Accounts""
                WHERE   ""Address"" = @address::character(36)
                LIMIT   1";

            using var db = GetConnection();
            var item = await db.QueryFirstOrDefaultAsync(sql, new { address });
            
            if (item == null)
            {
                return new User
                {
                    Address = address,
                    Balance = 0,
                    Counter = await State.GetCounter()
                };
            }

            var balance = item.Balance;
            var delegateId = (int?)item.DelegateId;
            return new User
            {
                Alias = Aliases[item.Id].Name,
                Address = address,
                PublicKey = item.PublicKey,
                Balance = balance,
                Counter = balance > 0 ? item.Counter : await State.GetCounter(),
                Delegate = delegateId == null ? null : new DelegateInfo(Aliases[(int)delegateId], item.Staked),
                FirstActivity = item.FirstLevel,
                LastActivity = item.LastLevel
            };
        }

        public async Task<IEnumerable<User>> Get(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT  ""Id"", ""Address"", ""FirstLevel"", ""LastLevel"", ""Balance"", ""Counter"", ""DelegateId"", ""Staked"", ""PublicKey""
                FROM     ""Accounts""
                WHERE    ""Type"" < 2
                ORDER BY ""Id""
                OFFSET   @offset
                LIMIT    @limit";

            using var db = GetConnection();
            var items = await db.QueryAsync(sql, new { limit, offset });

            var users = new List<User>(items.Count());
            foreach (var item in items)
            {
                var balance = item.Balance;
                var delegateId = (int?)item.DelegateId;
                users.Add(new User
                {
                    Alias = Aliases[item.Id].Name,
                    Address = item.Address,
                    PublicKey = item.PublicKey,
                    Balance = balance,
                    Counter = balance > 0 ? item.Counter : await State.GetCounter(),
                    Delegate = delegateId == null ? null : new DelegateInfo(Aliases[(int)delegateId], item.Staked),
                    FirstActivity = item.FirstLevel,
                    LastActivity = item.LastLevel
                });
            }

            return users;
        }
    }
}
