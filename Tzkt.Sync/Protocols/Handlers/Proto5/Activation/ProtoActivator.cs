using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class ProtoActivator : Proto4.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.BallotQuorumMin = parameters["quorum_min"]?.Value<int>() ?? 2000;
            protocol.BallotQuorumMax = parameters["quorum_max"]?.Value<int>() ?? 7000;
            protocol.ProposalQuorum = parameters["min_proposal_quorum"]?.Value<int>() ?? 500;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            protocol.BallotQuorumMin = 2000;
            protocol.BallotQuorumMax = 7000;
            protocol.ProposalQuorum = 500;
        }

        // Airdrop
        // Proposal invoice
        // Code change

        protected override async Task MigrateContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            var statistics = await Cache.Statistics.GetAsync(state.Level);

            #region airdrop
            var emptiedManagers = await Db.Contracts
                .AsNoTracking()
                .Include(x => x.Manager)
                .Where(x => x.Spendable == null &&
                            x.Manager.Type == AccountType.User &&
                            x.Manager.Balance == 0 &&
                            x.Manager.Counter > 0)
                .Select(x => x.Manager)
                .ToListAsync();

            var dict = new Dictionary<string, User>(8000);
            foreach (var manager in emptiedManagers)
                dict[manager.Address] = manager;

            foreach (var manager in dict.Values)
            {
                Db.TryAttach(manager);
                Cache.Accounts.Add(manager);

                manager.Balance = 1;
                manager.Counter = state.ManagerCounter;
                manager.MigrationsCount++;

                block.Operations |= Operations.Migrations;
                Db.MigrationOps.Add(new MigrationOperation
                {
                    Id = Cache.AppState.NextOperationId(),
                    Block = block,
                    Level = state.Level,
                    Timestamp = state.Timestamp,
                    Account = manager,
                    Kind = MigrationKind.AirDrop,
                    BalanceChange = 1
                });
            }

            state.MigrationOpsCount += dict.Values.Count;
            statistics.TotalCreated += dict.Values.Count;
            #endregion

            #region invoice
            var account = await Cache.Accounts.GetAsync("KT1DUfaMfTRZZkvZAYQT5b3byXnvqoAykc43");

            Db.TryAttach(account);
            account.Balance += 500_000_000;
            account.MigrationsCount++;

            block.Operations |= Operations.Migrations;
            Db.MigrationOps.Add(new MigrationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                Account = account,
                Kind = MigrationKind.ProposalInvoice,
                BalanceChange = 500_000_000
            });

            state.MigrationOpsCount++;
            statistics.TotalCreated += 500_000_000;
            #endregion

            #region scripts
            var smartContracts = await Db.Contracts
                .Where(x => x.Kind > ContractKind.DelegatorContract)
                .ToListAsync();

            var scripts = (await Db.Scripts
                .AsNoTracking()
                .ToListAsync())
                .ToDictionary(x => x.ContractId);

            foreach (var contract in smartContracts)
            {
                Cache.Accounts.Add(contract);

                var script = scripts[contract.Id];
                var storage = await Cache.Storages.GetAsync(contract);
                var rawContract = await Proto.Rpc.GetContractAsync(block.Level, contract.Address);

                var code = Micheline.FromJson(rawContract.Required("script").Required("code")) as MichelineArray;
                var micheParameter = code.First(x => x is MichelinePrim p && p.Prim == PrimType.parameter).ToBytes();
                var micheStorage = code.First(x => x is MichelinePrim p && p.Prim == PrimType.storage).ToBytes();
                var micheCode = code.First(x => x is MichelinePrim p && p.Prim == PrimType.code).ToBytes();
                var micheViews = code.Where(x => x is MichelinePrim p && p.Prim == PrimType.view);

                var newSchema = new Netezos.Contracts.ContractScript(code);
                var newStorageValue = Micheline.FromJson(rawContract.Required("script").Required("storage"));
                var newRawStorageValue = newSchema.OptimizeStorage(newStorageValue, false).ToBytes();

                if (script.ParameterSchema.IsEqual(micheParameter) &&
                    script.StorageSchema.IsEqual(micheStorage) &&
                    script.CodeSchema.IsEqual(micheCode) &&
                    storage.RawValue.IsEqual(newRawStorageValue))
                    continue;

                Db.TryAttach(script);
                script.Current = false;

                Db.TryAttach(storage);
                storage.Current = false;

                var migration = new MigrationOperation
                {
                    Id = Cache.AppState.NextOperationId(),
                    Block = block,
                    Level = block.Level,
                    Timestamp = block.Timestamp,
                    Account = contract,
                    Kind = MigrationKind.CodeChange
                };
                var newScript = new Script
                {
                    Id = Cache.AppState.NextScriptId(),
                    Level = migration.Level,
                    ContractId = contract.Id,
                    MigrationId = migration.Id,
                    ParameterSchema = micheParameter,
                    StorageSchema = micheStorage,
                    CodeSchema = micheCode,
                    Views = micheViews.Any()
                        ? micheViews.Select(x => x.ToBytes()).ToArray()
                        : null,
                    Current = true
                };
                var newStorage = new Storage
                {
                    Id = Cache.AppState.NextStorageId(),
                    Level = migration.Level,
                    ContractId = contract.Id,
                    MigrationId = migration.Id,
                    RawValue = newRawStorageValue,
                    JsonValue = newScript.Schema.HumanizeStorage(newStorageValue),
                    Current = true
                };

                var viewsBytes = newScript.Views?
                    .OrderBy(x => x, new BytesComparer())
                    .SelectMany(x => x)
                    .ToArray()
                    ?? Array.Empty<byte>();
                var typeSchema = newScript.ParameterSchema.Concat(newScript.StorageSchema).Concat(viewsBytes);
                var fullSchema = typeSchema.Concat(newScript.CodeSchema);
                contract.TypeHash = newScript.TypeHash = Script.GetHash(typeSchema);
                contract.CodeHash = newScript.CodeHash = Script.GetHash(fullSchema);

                migration.Script = newScript;
                migration.Storage = newStorage;

                contract.MigrationsCount++;
                state.MigrationOpsCount++;

                Db.MigrationOps.Add(migration);

                Db.Scripts.Add(newScript);
                Cache.Schemas.Add(contract, newScript.Schema);

                Db.Storages.Add(newStorage);
                Cache.Storages.Add(contract, newStorage);

                var tree = script.Schema.Storage.Schema.ToTreeView(Micheline.FromBytes(storage.RawValue));
                var bigmap = tree.Nodes().FirstOrDefault(x => x.Schema.Prim == PrimType.big_map);
                if (bigmap != null)
                {
                    var newTree = newScript.Schema.Storage.Schema.ToTreeView(Micheline.FromBytes(newStorage.RawValue));
                    var newBigmap = newTree.Nodes().FirstOrDefault(x => x.Schema.Prim == PrimType.big_map);
                    if (newBigmap.Value is not MichelineInt mi)
                        throw new System.Exception("Expected micheline int");
                    var newPtr = (int)mi.Value;

                    if (newBigmap.Path != bigmap.Path)
                        await Db.Database.ExecuteSqlRawAsync($@"
                            UPDATE ""BigMaps"" SET ""StoragePath"" = '{newBigmap.Path}' WHERE ""Ptr"" = {contract.Id};
                        ");

                    await Db.Database.ExecuteSqlRawAsync($@"
                        UPDATE ""BigMaps"" SET ""Ptr"" = {newPtr} WHERE ""Ptr"" = {contract.Id};
                        UPDATE ""BigMapKeys"" SET ""BigMapPtr"" = {newPtr} WHERE ""BigMapPtr"" = {contract.Id};
                        UPDATE ""BigMapUpdates"" SET ""BigMapPtr"" = {newPtr} WHERE ""BigMapPtr"" = {contract.Id};
                    ");

                    var storages = await Db.Storages.Where(x => x.ContractId == contract.Id).ToListAsync();
                    foreach (var prevStorage in storages)
                    {
                        var prevValue = Micheline.FromBytes(prevStorage.RawValue);
                        var prevTree = script.Schema.Storage.Schema.ToTreeView(prevValue);
                        var prevBigmap = prevTree.Nodes().First(x => x.Schema.Prim == PrimType.big_map);
                        (prevBigmap.Value as MichelineInt).Value = newPtr;

                        prevStorage.RawValue = prevValue.ToBytes();
                        prevStorage.JsonValue = script.Schema.HumanizeStorage(prevValue);
                    }
                }
            }
            #endregion
        }

        protected override async Task RevertContext(AppState state)
        {
            #region airdrop
            var airDrops = await Db.MigrationOps
                .AsNoTracking()
                .Include(x => x.Account)
                .Where(x => x.Kind == MigrationKind.AirDrop)
                .ToListAsync();

            foreach (var airDrop in airDrops)
            {
                Db.TryAttach(airDrop.Account);
                Cache.Accounts.Add(airDrop.Account);

                airDrop.Account.Balance = 0;
                airDrop.Account.MigrationsCount--;
            }

            Db.MigrationOps.RemoveRange(airDrops);
            Cache.AppState.ReleaseOperationId(airDrops.Count);

            state.MigrationOpsCount -= airDrops.Count;
            #endregion

            #region invoice
            var invoice = await Db.MigrationOps
                .AsNoTracking()
                .Include(x => x.Account)
                .FirstOrDefaultAsync(x => x.Level == state.Level && x.Kind == MigrationKind.ProposalInvoice);

            Db.TryAttach(invoice.Account);
            Cache.Accounts.Add(invoice.Account);

            invoice.Account.Balance -= 500_000_000;
            invoice.Account.MigrationsCount--;

            Db.MigrationOps.Remove(invoice);
            Cache.AppState.ReleaseOperationId();

            state.MigrationOpsCount--;
            #endregion

            #region scripts
            var codeChanges = await Db.MigrationOps
                .Include(x => x.Account)
                .Include(x => x.Script)
                .Include(x => x.Storage)
                .Where(x => x.Kind == MigrationKind.CodeChange)
                .ToListAsync();

            var oldScripts = (await Db.Scripts.Where(x => !x.Current)
                .ToListAsync())
                .ToDictionary(x => x.ContractId);

            foreach (var change in codeChanges)
            {
                var contract = change.Account as Contract;
                Cache.Accounts.Add(contract);

                var oldScript = oldScripts[contract.Id];
                var oldStorage = await Db.Storages
                    .Where(x => x.ContractId == contract.Id && x.Id < change.StorageId)
                    .OrderByDescending(x => x.Id)
                    .FirstAsync();

                var tree = change.Script.Schema.Storage.Schema.ToTreeView(Micheline.FromBytes(change.Storage.RawValue));
                var bigmap = tree.Nodes().FirstOrDefault(x => x.Schema.Prim == PrimType.big_map);
                if (bigmap != null)
                {
                    var oldTree = oldScript.Schema.Storage.Schema.ToTreeView(Micheline.FromBytes(oldStorage.RawValue));
                    var oldBigmap = oldTree.Nodes().FirstOrDefault(x => x.Schema.Prim == PrimType.big_map);

                    if (bigmap.Value is not MichelineInt mi)
                        throw new System.Exception("Expected micheline int");
                    var newPtr = (int)mi.Value;

                    if (oldBigmap.Path != bigmap.Path)
                        await Db.Database.ExecuteSqlRawAsync($@"
                            UPDATE ""BigMaps"" SET ""StoragePath"" = '{oldBigmap.Path}' WHERE ""Ptr"" = {newPtr};
                        ");

                    await Db.Database.ExecuteSqlRawAsync($@"
                        UPDATE ""BigMaps"" SET ""Ptr"" = {contract.Id} WHERE ""Ptr"" = {newPtr};
                        UPDATE ""BigMapKeys"" SET ""BigMapPtr"" = {contract.Id} WHERE ""BigMapPtr"" = {newPtr};
                        UPDATE ""BigMapUpdates"" SET ""BigMapPtr"" = {contract.Id} WHERE ""BigMapPtr"" = {newPtr};
                    ");

                    var storages = await Db.Storages.Where(x => x.ContractId == contract.Id && x.Level < change.Level).ToListAsync();
                    foreach (var prevStorage in storages)
                    {
                        var prevValue = Micheline.FromBytes(prevStorage.RawValue);
                        var prevTree = oldScript.Schema.Storage.Schema.ToTreeView(prevValue);
                        var prevBigmap = prevTree.Nodes().First(x => x.Schema.Prim == PrimType.big_map);
                        (prevBigmap.Value as MichelineInt).Value = contract.Id;

                        prevStorage.RawValue = prevValue.ToBytes();
                        prevStorage.JsonValue = oldScript.Schema.HumanizeStorage(prevValue);
                    }
                }

                oldScript.Current = true;
                Cache.Schemas.Add(contract, oldScript.Schema);

                oldStorage.Current = true;
                Cache.Storages.Add(contract, oldStorage);

                Db.Scripts.Remove(change.Script);
                Cache.AppState.ReleaseScriptId();
                Db.Storages.Remove(change.Storage);
                Cache.AppState.ReleaseStorageId();

                contract.TypeHash = oldScript.TypeHash;
                contract.CodeHash = oldScript.CodeHash;
                contract.MigrationsCount--;
            }

            Db.MigrationOps.RemoveRange(codeChanges);
            Cache.AppState.ReleaseOperationId(codeChanges.Count);
            state.MigrationOpsCount -= codeChanges.Count;
            #endregion
        }
    }
}
