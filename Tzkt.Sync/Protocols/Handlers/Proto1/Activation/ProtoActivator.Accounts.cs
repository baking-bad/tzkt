using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
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
                    DeactivationLevel = GracePeriod.Init(2, protocol),
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

                #region script
                var code = Micheline.FromJson(data[1].Required("code")) as MichelineArray;
                var micheParameter = code.First(x => x is MichelinePrim p && p.Prim == PrimType.parameter);
                var micheStorage = code.First(x => x is MichelinePrim p && p.Prim == PrimType.storage);
                var micheCode = code.First(x => x is MichelinePrim p && p.Prim == PrimType.code);
                var script = new Script
                {
                    Id = Cache.AppState.NextScriptId(),
                    Level = 1,
                    ContractId = contract.Id,
                    ParameterSchema = micheParameter.ToBytes(),
                    StorageSchema = micheStorage.ToBytes(),
                    CodeSchema = micheCode.ToBytes(),
                    Current = true
                };

                var typeSchema = script.ParameterSchema.Concat(script.StorageSchema);
                var fullSchema = typeSchema.Concat(script.CodeSchema);
                contract.TypeHash = script.TypeHash = Script.GetHash(typeSchema);
                contract.CodeHash = script.CodeHash = Script.GetHash(fullSchema);

                Db.Scripts.Add(script);
                Cache.Schemas.Add(contract, script.Schema);

                var storageValue = Micheline.FromJson(data[1].Required("storage"));
                var storage = new Storage
                {
                    Id = Cache.AppState.NextStorageId(),
                    Level = 1,
                    ContractId = contract.Id,
                    RawValue = script.Schema.OptimizeStorage(storageValue, false).ToBytes(),
                    JsonValue = script.Schema.HumanizeStorage(storageValue),
                    Current = true
                };

                Db.Storages.Add(storage);
                Cache.Storages.Add(contract, storage);
                #endregion

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
                var migration = new MigrationOperation
                {
                    Id = Cache.AppState.NextOperationId(),
                    Block = block,
                    Level = block.Level,
                    Timestamp = block.Timestamp,
                    Account = account,
                    Kind = MigrationKind.Bootstrap,
                    BalanceChange = account.Balance,
                };

                if (account is Contract contract)
                {
                    var script = Db.ChangeTracker.Entries()
                        .First(x => x.Entity is Script s && s.ContractId == contract.Id).Entity as Script;
                    var storage = await Cache.Storages.GetAsync(contract);
                    
                    script.MigrationId = migration.Id;
                    storage.MigrationId = migration.Id;

                    migration.Script = script;
                    migration.Storage = storage;
                }

                Db.MigrationOps.Add(migration);
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
                DELETE FROM ""MigrationOps"";
                DELETE FROM ""Scripts"";
                DELETE FROM ""Storages"";");

            await Cache.Accounts.ResetAsync();
            Cache.Schemas.Reset();
            Cache.Storages.Reset();

            var state = Cache.AppState.Get();
            state.AccountsCount = 0;
            state.MigrationOpsCount = 0;
        }
    }
}
