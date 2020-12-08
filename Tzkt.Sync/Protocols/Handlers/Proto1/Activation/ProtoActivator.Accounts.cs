using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        async Task<List<Account>> BootstrapAccounts(Protocol protocol)
        {
            var rawAccounts = await Proto.Rpc.GetAllContractsAsync(1);
            var accounts = new List<Account>(65);

            #region bootstrap delegates
            foreach (var data in rawAccounts
                .EnumerateArray()
                .Where(x => x[0].RequiredString() == x[1].OptionalString("delegate")))
            {
                var baker = new Delegate
                {
                    Id = Cache.AppState.NextAccountId(),
                    Address = data[0].RequiredString(),
                    Balance = data[1].RequiredInt64("balance"),
                    StakingBalance = data[1].RequiredInt64("balance"),
                    Counter = data[1].RequiredInt32("counter"),
                    PublicKey = data[1].RequiredString("manager"),
                    FirstLevel = 1,
                    LastLevel = 1,
                    ActivationLevel = 1,
                    DeactivationLevel = GracePeriod.Init(1, protocol.BlocksPerCycle, protocol.PreservedCycles),
                    Staked = true,
                    Revealed = true,
                    Type = AccountType.Delegate
                };
                Cache.Accounts.Add(baker);
                accounts.Add(baker);
            }
            #endregion

            #region bootstrap users
            foreach (var data in rawAccounts
                .EnumerateArray()
                .Where(x => x[0].RequiredString()[0] == 't' && x[1].OptionalString("delegate") == null))
            {
                var user = new User
                {
                    Id = Cache.AppState.NextAccountId(),
                    Address = data[0].RequiredString(),
                    Balance = data[1].RequiredInt64("balance"),
                    Counter = data[1].RequiredInt32("counter"),
                    FirstLevel = 1,
                    LastLevel = 1,
                    Type = AccountType.User
                };
                Cache.Accounts.Add(user);
                accounts.Add(user);
            }
            #endregion

            #region bootstrap contracts
            foreach (var data in rawAccounts.EnumerateArray().Where(x => x[0].RequiredString()[0] == 'K'))
            {
                var delegat = Cache.Accounts.GetDelegate(data[1].OptionalString("delegate"));
                var manager = (User)await Cache.Accounts.GetAsync(data[1].RequiredString("manager"));

                var contract = new Contract
                {
                    Id = Cache.AppState.NextAccountId(),
                    Address = data[0].RequiredString(),
                    Balance = data[1].RequiredInt64("balance"),
                    Counter = data[1].RequiredInt32("counter"),
                    FirstLevel = 1,
                    LastLevel = 1,
                    Spendable = false,
                    DelegationLevel = 1,
                    Delegate = delegat,
                    Manager = manager,
                    Staked = !string.IsNullOrEmpty(data[1].OptionalString("delegate")),
                    Type = AccountType.Contract,
                    Kind = ContractKind.SmartContract,
                };

                manager.ContractsCount++;
                delegat.DelegatorsCount++;
                delegat.StakingBalance += contract.Balance;

                Cache.Accounts.Add(contract);
                accounts.Add(contract);
            }
            #endregion

            Db.Accounts.AddRange(accounts);

            #region migration ops
            var block = Cache.Blocks.Current();

            block.Operations |= Operations.Migrations;
            if (accounts.Any(x => x.Type == AccountType.Contract))
                block.Events |= BlockEvents.SmartContracts;

            foreach (var account in accounts)
            {
                Db.MigrationOps.Add(new MigrationOperation
                {
                    Id = Cache.AppState.NextOperationId(),
                    Block = block,
                    Level = block.Level,
                    Timestamp = block.Timestamp,
                    Account = account,
                    Kind = MigrationKind.Bootstrap,
                    BalanceChange = account.Balance
                });
                account.MigrationsCount++;
            }

            var state = Cache.AppState.Get();
            state.MigrationOpsCount += accounts.Count;
            #endregion

            #region statistics
            var stats = await Cache.Statistics.GetAsync(1);
            stats.TotalBootstrapped = accounts.Sum(x => x.Balance);
            stats.TotalVested = accounts.Where(x => x.Type == AccountType.Contract).Sum(x => x.Balance);
            #endregion

            return accounts;
        }

        async Task ClearAccounts()
        {
            await Db.Database.ExecuteSqlRawAsync(@"
                DELETE FROM ""Accounts"";
                DELETE FROM ""MigrationOps"";");

            await Cache.Accounts.ResetAsync();

            var state = Cache.AppState.Get();
            state.AccountsCount = 0;
            state.MigrationOpsCount = 0;
        }
    }
}
