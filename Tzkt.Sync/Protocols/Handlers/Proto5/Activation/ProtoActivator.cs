using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
using Newtonsoft.Json.Linq;
using System.Security.Principal;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class ProtoActivator(ProtocolHandler proto) : Proto4.ProtoActivator(proto)
    {
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
            Db.TryAttach(block);

            var statistics = Cache.Statistics.Current;
            Db.TryAttach(statistics);

            #region airdrop
            var managers = File.ReadAllLines("./Protocols/Handlers/Proto5/Activation/airdropped.contracts");

            if (state.Chain == "mainnet")
                await Cache.Accounts.LoadAsync(managers);
            else
                await Cache.Accounts.Preload(managers);

            foreach (var address in managers)
            {
                if (Cache.Accounts.TryGetCached(address, out var manager))
                {
                    Db.TryAttach(manager);

                    Receive(manager, 1);
                    manager.Counter = state.ManagerCounter;
                    manager.MigrationsCount++;
                    manager.LastLevel = block.Level;

                    block.Operations |= Operations.Migrations;

                    var airdropMigration = new MigrationOperation
                    {
                        Id = Cache.AppState.NextOperationId(),
                        Level = state.Level,
                        Timestamp = state.Timestamp,
                        AccountId = manager.Id,
                        Kind = MigrationKind.AirDrop,
                        BalanceChange = 1
                    };
                    Db.MigrationOps.Add(airdropMigration);
                    Context.MigrationOps.Add(airdropMigration);

                    state.MigrationOpsCount++;
                    statistics.TotalCreated += airdropMigration.BalanceChange;
                }
            }
            #endregion

            #region invoice
            var account = (await Cache.Accounts.GetAsync("KT1DUfaMfTRZZkvZAYQT5b3byXnvqoAykc43"))!;
            Db.TryAttach(account);
            Receive(account, 500_000_000);
            account.MigrationsCount++;
            account.LastLevel = block.Level;

            block.Operations |= Operations.Migrations;

            var invoiceMigration = new MigrationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                AccountId = account.Id,
                Kind = MigrationKind.ProposalInvoice,
                BalanceChange = 500_000_000
            };
            Db.MigrationOps.Add(invoiceMigration);
            Context.MigrationOps.Add(invoiceMigration);

            state.MigrationOpsCount++;
            statistics.TotalCreated += 500_000_000;
            #endregion

            #region scripts
            var contracts = await Db.Contracts.ToListAsync(); // ~27k
            var scripts = await Db.Scripts.Where(x => x.Current).ToDictionaryAsync(x => x.ContractId);
            var storages = await Db.Storages.Where(x => x.Current).ToDictionaryAsync(x => x.ContractId);
            var originations = await Db.OriginationOps.Where(x => x.ContractId != null).ToDictionaryAsync(x => x.ContractId!.Value);

            Cache.Schemas.Reset();
            Cache.Storages.Reset();

            foreach (var contract in contracts)
            {
                Cache.Accounts.Update(contract);

                if (contract.Kind == ContractKind.DelegatorContract)
                {
                    var script = scripts[contract.Id];
                    script.Level = block.Level;
                    script.OriginationId = null;

                    var storage = storages[contract.Id];
                    storage.Level = block.Level;
                    storage.OriginationId = null;

                    var origination = originations[contract.Id];
                    origination.ScriptId = null;
                    origination.StorageId = null;

                    var migration = new MigrationOperation
                    {
                        Id = Cache.AppState.NextOperationId(),
                        Level = block.Level,
                        Timestamp = block.Timestamp,
                        AccountId = contract.Id,
                        Kind = MigrationKind.CodeChange,
                        ScriptId = script.Id,
                        StorageId = storage.Id
                    };

                    script.MigrationId = migration.Id;
                    storage.MigrationId = migration.Id;

                    contract.MigrationsCount++;
                    contract.LastLevel = block.Level;

                    state.MigrationOpsCount++;

                    Db.MigrationOps.Add(migration);
                    Context.MigrationOps.Add(migration);
                }
                else
                {
                    var script = scripts[contract.Id];
                    var storage = storages[contract.Id];

                    var rawContract = await Proto.Rpc.GetContractAsync(block.Level, contract.Address);

                    var code = (Micheline.FromJson(rawContract.Required("script").Required("code")) as MichelineArray)!;
                    var micheParameter = code.First(x => x is MichelinePrim p && p.Prim == PrimType.parameter).ToBytes();
                    var micheStorage = code.First(x => x is MichelinePrim p && p.Prim == PrimType.storage).ToBytes();
                    var micheCode = code.First(x => x is MichelinePrim p && p.Prim == PrimType.code).ToBytes();
                    var micheViews = code.Where(x => x is MichelinePrim p && p.Prim == PrimType.view);

                    var newSchema = new Netezos.Contracts.ContractScript(code);
                    var newStorageValue = Micheline.FromJson(rawContract.Required("script").Required("storage"))!;
                    var newRawStorageValue = newSchema.OptimizeStorage(newStorageValue, false).ToBytes();

                    if (script.ParameterSchema.IsEqual(micheParameter) &&
                        script.StorageSchema.IsEqual(micheStorage) &&
                        script.CodeSchema.IsEqual(micheCode) &&
                        storage.RawValue.IsEqual(newRawStorageValue))
                        continue;

                    script.Current = false;
                    storage.Current = false;

                    var migration = new MigrationOperation
                    {
                        Id = Cache.AppState.NextOperationId(),
                        Level = block.Level,
                        Timestamp = block.Timestamp,
                        AccountId = contract.Id,
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
                        ?? [];
                    var typeSchema = newScript.ParameterSchema.Concat(newScript.StorageSchema).Concat(viewsBytes);
                    var fullSchema = typeSchema.Concat(newScript.CodeSchema);
                    contract.TypeHash = newScript.TypeHash = Script.GetHash(typeSchema);
                    contract.CodeHash = newScript.CodeHash = Script.GetHash(fullSchema);

                    migration.ScriptId = newScript.Id;
                    migration.StorageId = newStorage.Id;

                    contract.MigrationsCount++;
                    contract.LastLevel = block.Level;

                    state.MigrationOpsCount++;

                    Db.MigrationOps.Add(migration);
                    Context.MigrationOps.Add(migration);

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
                        if (newBigmap?.Value is not MichelineInt micheInt)
                            throw new Exception("Expected micheline int");
                        var newPtr = (int)micheInt.Value;

                        if (newBigmap.Path != bigmap.Path)
                            await Db.Database.ExecuteSqlRawAsync("""
                                UPDATE "BigMaps"
                                SET "StoragePath" = {0}
                                WHERE "Ptr" = {1}
                                """, newBigmap.Path, contract.Id);

                        await Db.Database.ExecuteSqlRawAsync("""
                            UPDATE "BigMaps" SET "Ptr" = {0} WHERE "Ptr" = {1};
                            UPDATE "BigMapKeys" SET "BigMapPtr" = {0} WHERE "BigMapPtr" = {1};
                            UPDATE "BigMapUpdates" SET "BigMapPtr" = {0} WHERE "BigMapPtr" = {1};
                            """, newPtr, contract.Id);

                        foreach (var prevStorage in await Db.Storages.Where(x => x.ContractId == contract.Id).ToListAsync())
                        {
                            var prevValue = Micheline.FromBytes(prevStorage.RawValue);
                            var prevTree = script.Schema.Storage.Schema.ToTreeView(prevValue);
                            var prevBigmap = prevTree.Nodes().First(x => x.Schema.Prim == PrimType.big_map);
                            (prevBigmap.Value as MichelineInt)!.Value = newPtr;

                            prevStorage.RawValue = prevValue.ToBytes();
                            prevStorage.JsonValue = script.Schema.HumanizeStorage(prevValue);
                        }
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
                .Where(x => x.Kind == MigrationKind.AirDrop)
                .ToListAsync();

            foreach (var airDrop in airDrops)
            {
                var account = await Cache.Accounts.GetAsync(airDrop.AccountId);
                Db.TryAttach(account);

                RevertReceive(account, 1);
                account.MigrationsCount--;
            }

            Db.MigrationOps.RemoveRange(airDrops);
            Cache.AppState.ReleaseOperationId(airDrops.Count);

            state.MigrationOpsCount -= airDrops.Count;
            #endregion

            #region invoice
            var invoice = await Db.MigrationOps
                .AsNoTracking()
                .FirstAsync(x => x.Level == state.Level && x.Kind == MigrationKind.ProposalInvoice);

            var invoiceAccount = await Cache.Accounts.GetAsync(invoice.AccountId);
            Db.TryAttach(invoiceAccount);

            RevertReceive(invoiceAccount, 500_000_000);
            invoiceAccount.MigrationsCount--;

            Db.MigrationOps.Remove(invoice);
            Cache.AppState.ReleaseOperationId();

            state.MigrationOpsCount--;
            #endregion

            #region scripts
            var contracts = await Db.Contracts.ToDictionaryAsync(x => x.Id); // ~27k
            var scripts = await Db.Scripts.Where(x => x.Current).ToDictionaryAsync(x => x.ContractId);
            var storages = await Db.Storages.Where(x => x.Current).ToDictionaryAsync(x => x.ContractId);
            var originations = await Db.OriginationOps.Where(x => x.ContractId != null).ToDictionaryAsync(x => x.ContractId!.Value);

            var codeChanges = await Db.MigrationOps.Where(x => x.Kind == MigrationKind.CodeChange).ToListAsync();

            Cache.Schemas.Reset();
            Cache.Storages.Reset();

            foreach (var change in codeChanges)
            {
                var contract = contracts[change.AccountId];
                Cache.Accounts.Update(contract);

                if (contract.Kind == ContractKind.DelegatorContract)
                {
                    var origination = originations[contract.Id];

                    var script = scripts[contract.Id];
                    script.Level = origination.Level;
                    script.OriginationId = origination.Id;

                    var storage = storages[contract.Id];
                    storage.Level = origination.Level;
                    storage.OriginationId = origination.Id;

                    origination.ScriptId = script.Id;
                    origination.StorageId = storage.Id;

                    script.MigrationId = null;
                    storage.MigrationId = null;

                    contract.MigrationsCount--;
                    contract.LastLevel = state.Level;
                }
                else
                {
                    var script = scripts[contract.Id];
                    var storage = storages[contract.Id];

                    var oldScript = await Db.Scripts
                        .Where(x => x.ContractId == contract.Id && x.Id < script.Id)
                        .OrderByDescending(x => x.Id)
                        .FirstAsync();

                    var oldStorage = await Db.Storages
                        .Where(x => x.ContractId == contract.Id && x.Id < storage.Id)
                        .OrderByDescending(x => x.Id)
                        .FirstAsync();

                    var tree = script.Schema.Storage.Schema.ToTreeView(Micheline.FromBytes(storage.RawValue));
                    var bigmap = tree.Nodes().FirstOrDefault(x => x.Schema.Prim == PrimType.big_map);
                    if (bigmap != null)
                    {
                        var oldTree = oldScript.Schema.Storage.Schema.ToTreeView(Micheline.FromBytes(oldStorage.RawValue));
                        var oldBigmap = oldTree.Nodes().First(x => x.Schema.Prim == PrimType.big_map);

                        if (bigmap.Value is not MichelineInt mi)
                            throw new Exception("Expected micheline int");
                        var newPtr = (int)mi.Value;

                        if (oldBigmap.Path != bigmap.Path)
                            await Db.Database.ExecuteSqlRawAsync("""
                                UPDATE "BigMaps"
                                SET "StoragePath" = {0}
                                WHERE "Ptr" = {1}
                                """, oldBigmap.Path, newPtr);

                        await Db.Database.ExecuteSqlRawAsync("""
                            UPDATE "BigMaps" SET "Ptr" = {0} WHERE "Ptr" = {1};
                            UPDATE "BigMapKeys" SET "BigMapPtr" = {0} WHERE "BigMapPtr" = {1};
                            UPDATE "BigMapUpdates" SET "BigMapPtr" = {0} WHERE "BigMapPtr" = {1};
                            """, contract.Id, newPtr);

                        foreach (var prevStorage in await Db.Storages.Where(x => x.ContractId == contract.Id && x.Level < change.Level).ToListAsync())
                        {
                            var prevValue = Micheline.FromBytes(prevStorage.RawValue);
                            var prevTree = oldScript.Schema.Storage.Schema.ToTreeView(prevValue);
                            var prevBigmap = prevTree.Nodes().First(x => x.Schema.Prim == PrimType.big_map);
                            (prevBigmap.Value as MichelineInt)!.Value = contract.Id;

                            prevStorage.RawValue = prevValue.ToBytes();
                            prevStorage.JsonValue = oldScript.Schema.HumanizeStorage(prevValue);
                        }
                    }

                    oldScript.Current = true;
                    Cache.Schemas.Add(contract, oldScript.Schema);

                    oldStorage.Current = true;
                    Cache.Storages.Add(contract, oldStorage);

                    Db.Scripts.Remove(script);
                    Cache.AppState.ReleaseScriptId();

                    Db.Storages.Remove(storage);
                    Cache.AppState.ReleaseStorageId();

                    contract.TypeHash = oldScript.TypeHash;
                    contract.CodeHash = oldScript.CodeHash;
                    contract.MigrationsCount--;
                }
            }

            Db.MigrationOps.RemoveRange(codeChanges);
            Cache.AppState.ReleaseOperationId(codeChanges.Count);
            state.MigrationOpsCount -= codeChanges.Count;
            #endregion
        }
    }
}
