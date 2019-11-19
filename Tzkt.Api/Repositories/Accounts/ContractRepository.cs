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
    public class ContractRepository : DbConnection
    {
        readonly AliasService Aliases;

        public ContractRepository(AliasService aliases, IConfiguration config) : base(config)
        {
            Aliases = aliases;
        }

        public async Task<Contract> Get(string address)
        {
            var sql = @"
                SELECT      acc.""Id"", acc.""FirstLevel"", acc.""LastLevel"", acc.""Balance"",
                            acc.""DelegateId"", acc.""Staked"", acc.""Kind"", acc.""ManagerId"", mgr.""PublicKey""
                FROM        ""Accounts"" as acc
                LEFT JOIN   ""Accounts"" as mgr
                ON          mgr.""Id"" = acc.""ManagerId""
                WHERE       acc.""Address"" = @address::character(36)
                LIMIT       1";

            using var db = GetConnection();
            var item = await db.QueryFirstOrDefaultAsync(sql, new { address });
            if (item == null) return null;

            var kind = (int)item.Kind;
            var delegateId = (int?)item.DelegateId;
            return new Contract
            {
                Kind = kind switch
                {
                    0 => "delegator_contract",
                    1 => "smart_contract",
                    _ => "unknown"
                },
                Alias = Aliases[item.Id].Name,
                Address = address,
                Balance = item.Balance,
                Delegate = delegateId == null ? null : new DelegateInfo(Aliases[(int)delegateId], item.Staked),
                Manager = new ManagerInfo(Aliases[(int)item.ManagerId], item.PublicKey),
                FirstActivity = item.FirstLevel,
                LastActivity = item.LastLevel
            };
        }

        public async Task<IEnumerable<Contract>> Get(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT      acc.""Id"", acc.""Address"", acc.""FirstLevel"", acc.""LastLevel"", acc.""Balance"",
                            acc.""DelegateId"", acc.""Staked"", acc.""Kind"", acc.""ManagerId"", mgr.""PublicKey""
                FROM        ""Accounts"" as acc
                LEFT JOIN   ""Accounts"" as mgr
                ON          mgr.""Id"" = acc.""ManagerId""
                WHERE       acc.""Type"" = 2
                ORDER BY    acc.""Id""
                OFFSET      @offset
                LIMIT       @limit";

            using var db = GetConnection();
            var items = await db.QueryAsync(sql, new { limit, offset });

            var accounts = new List<Contract>(items.Count());
            foreach (var item in items)
            {
                var kind = (int)item.Kind;
                var delegateId = (int?)item.DelegateId;
                accounts.Add(new Contract
                {
                    Kind = kind switch
                    {
                        0 => "delegator_contract",
                        1 => "smart_contract",
                        _ => "unknown"
                    },
                    Alias = Aliases[item.Id].Name,
                    Address = item.Address,
                    Balance = item.Balance,
                    Delegate = delegateId == null ? null : new DelegateInfo(Aliases[(int)delegateId], item.Staked),
                    Manager = new ManagerInfo(Aliases[(int)item.ManagerId], item.PublicKey),
                    FirstActivity = item.FirstLevel,
                    LastActivity = item.LastLevel
                });
            }

            return accounts;
        }
    }
}
