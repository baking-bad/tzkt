using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services;

namespace Tzkt.Api.Repositories
{
    public class DelegateRepository : DbConnection
    {
        readonly AliasService Aliases;
        readonly StateService State;

        public DelegateRepository(AliasService aliases, StateService state, IConfiguration config) : base(config)
        {
            Aliases = aliases;
            State = state;
        }

        public async Task<Delegate> Get(string address)
        {
            var sql = @"
                SELECT  ""Id"", ""FirstLevel"", ""LastLevel"", ""Balance"", ""Counter"", ""PublicKey"", ""ActivationLevel"", ""DeactivationLevel"",
                        ""FrozenDeposits"", ""FrozenRewards"", ""FrozenFees"", ""Delegators"", ""StakingBalance""
                FROM    ""Accounts""
                WHERE   ""Address"" = @address AND ""Type"" = 1
                LIMIT   1";

            using var db = GetConnection();
            var item = await db.QueryFirstOrDefaultAsync(sql, new { address });
            if (item == null) return null;

            var deactivation = (int?)item.DeactivationLevel;
            var active = deactivation > (await State.GetState()).Level;
            return new Delegate
            {
                Active = active,
                Alias = Aliases[item.Id].Name,
                Address = address,
                PublicKey = item.PublicKey,
                Balance = item.Balance,
                FrozenDeposits = item.FrozenDeposits,
                FrozenRewards = item.FrozenRewards,
                FrozenFees = item.FrozenFees,
                Counter = item.Counter,
                ActivationLevel = item.ActivationLevel,
                DeactivationLevel = active ? null : deactivation,
                DelegatorsCount = item.Delegators,
                StakingBalance = item.StakingBalance,
                FirstActivity = item.FirstLevel,
                LastActivity = item.LastLevel
            };
        }

        public async Task<IEnumerable<Delegate>> Get(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT  ""Id"", ""Address"", ""FirstLevel"", ""LastLevel"", ""Balance"", ""Counter"", ""PublicKey"", ""ActivationLevel"", ""DeactivationLevel"",
                        ""FrozenDeposits"", ""FrozenRewards"", ""FrozenFees"", ""Delegators"", ""StakingBalance""
                FROM     ""Accounts""
                WHERE    ""Type"" = 1
                ORDER BY ""Id""
                OFFSET   @offset
                LIMIT    @limit";

            using var db = GetConnection();
            var items = await db.QueryAsync(sql, new { limit, offset });

            var accounts = new List<Delegate>(items.Count());
            foreach (var item in items)
            {
                var deactivation = (int?)item.DeactivationLevel;
                var active = deactivation > (await State.GetState()).Level;
                accounts.Add(new Delegate
                {
                    Active = active,
                    Alias = Aliases[item.Id].Name,
                    Address = item.Address,
                    PublicKey = item.PublicKey,
                    Balance = item.Balance,
                    FrozenDeposits = item.FrozenDeposits,
                    FrozenRewards = item.FrozenRewards,
                    FrozenFees = item.FrozenFees,
                    Counter = item.Counter,
                    ActivationLevel = item.ActivationLevel,
                    DeactivationLevel = active ? null : deactivation,
                    DelegatorsCount = item.Delegators,
                    StakingBalance = item.StakingBalance,
                    FirstActivity = item.FirstLevel,
                    LastActivity = item.LastLevel
                });
            }

            return accounts;
        }
    }
}
