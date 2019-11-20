using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class AccountRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly AliasService Aliases;
        readonly StateService State;

        public AccountRepository(AccountsCache accounts, AliasService aliases, StateService state, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Aliases = aliases;
            State = state;
        }

        public async Task<IAccount> Get(string address)
        {
            var rawAccount = await Accounts.Get(address);

            if (rawAccount == null)
                return address[0] == 't'
                    ? new User
                    {
                        Address = address,
                        Counter = await State.GetCounter(),
                    }
                    : null;

            switch (rawAccount)
            {
                case RawDelegate delegat:
                    var deactivation = (int?)delegat.DeactivationLevel;
                    var active = deactivation > (await State.GetState()).Level;
                    return new Models.Delegate
                    {
                        Active = active,
                        Alias = Aliases[delegat.Id].Name,
                        Address = address,
                        PublicKey = delegat.PublicKey,
                        Balance = delegat.Balance,
                        FrozenDeposits = delegat.FrozenDeposits,
                        FrozenRewards = delegat.FrozenRewards,
                        FrozenFees = delegat.FrozenFees,
                        Counter = delegat.Counter,
                        ActivationLevel = delegat.ActivationLevel,
                        DeactivationLevel = active ? null : deactivation,
                        DelegatorsCount = delegat.Delegators,
                        StakingBalance = delegat.StakingBalance,
                        FirstActivity = delegat.FirstLevel,
                        LastActivity = delegat.LastLevel
                    };
                case RawUser user:
                    return new User
                    {
                        Alias = Aliases[user.Id].Name,
                        Address = address,
                        Balance = user.Balance,
                        Counter = user.Balance > 0 ? user.Counter : await State.GetCounter(),
                        FirstActivity = user.FirstLevel,
                        LastActivity = user.LastLevel,
                        PublicKey = user.PublicKey,
                        Delegate = user.DelegateId != null ? new DelegateInfo(Aliases[(int)user.DelegateId], user.Staked) : null
                    };
                case RawContract contract:
                    var managerAlias = Aliases[contract.ManagerId];
                    var manager = (RawUser)await Accounts.Get(managerAlias.Address);
                    return new Contract
                    {
                        Kind = KindToString(contract.Kind),
                        Alias = Aliases[contract.Id].Name,
                        Address = address,
                        Balance = contract.Balance,
                        Delegate = contract.DelegateId != null ? new DelegateInfo(Aliases[(int)contract.DelegateId], contract.Staked) : null,
                        Manager = new ManagerInfo(managerAlias, manager.PublicKey),
                        FirstActivity = contract.FirstLevel,
                        LastActivity = contract.LastLevel
                    };
                default:
                    throw new Exception($"Invalid raw account type");
            }
        }
        
        public async Task<IEnumerable<IAccount>> Get(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT      account.*, manager.""PublicKey"" as ""ManagerPublicKey""
                FROM        ""Accounts"" as account
                LEFT JOIN   ""Accounts"" as manager ON manager.""Id"" = account.""ManagerId""
                ORDER BY    account.""Id""
                OFFSET      @offset
                LIMIT       @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            var accounts = new List<IAccount>(rows.Count());
            foreach (var row in rows)
            {
                switch ((int)row.Type)
                {
                    case 0:
                        accounts.Add(new User
                        {
                            Alias = Aliases[row.Id].Name,
                            Address = row.Address,
                            Balance = row.Balance,
                            Counter = row.Balance > 0 ? row.Counter : await State.GetCounter(),
                            FirstActivity = row.FirstLevel,
                            LastActivity = row.LastLevel,
                            PublicKey = row.PublicKey,
                            Delegate = row.DelegateId != null ? new DelegateInfo(Aliases[row.DelegateId], row.Staked) : null
                        });
                        break;
                    case 1:
                        var deactivation = (int?)row.DeactivationLevel;
                        var active = deactivation > (await State.GetState()).Level;
                        accounts.Add(new Models.Delegate
                        {
                            Active = active,
                            Alias = Aliases[row.Id].Name,
                            Address = row.Address,
                            PublicKey = row.PublicKey,
                            Balance = row.Balance,
                            FrozenDeposits = row.FrozenDeposits,
                            FrozenRewards = row.FrozenRewards,
                            FrozenFees = row.FrozenFees,
                            Counter = row.Counter,
                            ActivationLevel = row.ActivationLevel,
                            DeactivationLevel = active ? null : deactivation,
                            DelegatorsCount = row.Delegators,
                            StakingBalance = row.StakingBalance,
                            FirstActivity = row.FirstLevel,
                            LastActivity = row.LastLevel
                        });
                        break;
                    case 2:
                        accounts.Add(new Contract
                        {
                            Kind = KindToString(row.Kind),
                            Alias = Aliases[row.Id].Name,
                            Address = row.Address,
                            Balance = row.Balance,
                            Delegate = row.DelegateId != null ? new DelegateInfo(Aliases[row.DelegateId], row.Staked) : null,
                            Manager = new ManagerInfo(Aliases[row.ManagerId], row.ManagerPublicKey),
                            FirstActivity = row.FirstLevel,
                            LastActivity = row.LastLevel
                        });
                        break;
                }
            }

            return accounts;
        }

        string KindToString(int kind) => kind switch
        {
            0 => "delegator_contract",
            1 => "smart_contract",
            _ => "unknown"
        };
    }
}
