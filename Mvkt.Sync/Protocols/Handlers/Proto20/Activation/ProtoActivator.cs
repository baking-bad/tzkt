using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netmavryk.Contracts;
using Netmavryk.Encoding;
using Newtonsoft.Json.Linq;
using Npgsql;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto20
{
    partial class ProtoActivator : Proto19.ProtoActivator
    {
        public const string ProtocolTreasuryContract = "KT1SxSxUqsop2ZaiNAJs3qDh33jjSLqz5Wqq";
        
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override async Task ActivateContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            await OriginateContract(block, ProtocolTreasuryContract);
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            // nothing to upgrade
        }

        protected override async Task MigrateContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            await OriginateContract(block, ProtocolTreasuryContract);
        }        

        protected override async Task RevertContext(AppState state)
        {
            await RemoveContract(ProtocolTreasuryContract);
        }

        async Task OriginateContract(Block block, string address)
        {
            var rawContract = await Proto.Rpc.GetContractAsync(block.Level, address);

            #region contract
            Contract contract;
            var ghost = await Cache.Accounts.GetAsync(address);
            if (ghost != null)
            {
                contract = new Contract
                {
                    Id = ghost.Id,
                    FirstLevel = ghost.FirstLevel,
                    LastLevel = block.Level,
                    Address = address,
                    Balance = rawContract.RequiredInt64("balance"),
                    Creator = await Cache.Accounts.GetAsync(NullAddress.Address),
                    Type = AccountType.Contract,
                    Kind = ContractKind.SmartContract,
                    MigrationsCount = 1,
                    ActiveTokensCount = ghost.ActiveTokensCount,
                    TokenBalancesCount = ghost.TokenBalancesCount,
                    TokenTransfersCount = ghost.TokenTransfersCount
                };
                Db.Entry(ghost).State = EntityState.Detached;
                Db.Entry(contract).State = EntityState.Modified;
            }
            else
            {
                contract = new Contract
                {
                    Id = Cache.AppState.NextAccountId(),
                    FirstLevel = block.Level,
                    LastLevel = block.Level,
                    Address = address,
                    Balance = rawContract.RequiredInt64("balance"),
                    Creator = await Cache.Accounts.GetAsync(NullAddress.Address),
                    Type = AccountType.Contract,
                    Kind = ContractKind.SmartContract,
                    MigrationsCount = 1,
                };
                Db.Accounts.Add(contract);
            }
            Cache.Accounts.Add(contract);

            Db.TryAttach(contract.Creator);
            contract.Creator.ContractsCount++;
            #endregion

            #region script
            var code = Micheline.FromJson(rawContract.Required("script").Required("code")) as MichelineArray;
            var micheParameter = code.First(x => x is MichelinePrim p && p.Prim == PrimType.parameter);
            var micheStorage = code.First(x => x is MichelinePrim p && p.Prim == PrimType.storage);
            var micheCode = code.First(x => x is MichelinePrim p && p.Prim == PrimType.code);
            var micheViews = code.Where(x => x is MichelinePrim p && p.Prim == PrimType.view);
            var script = new Script
            {
                Id = Cache.AppState.NextScriptId(),
                Level = block.Level,
                ContractId = contract.Id,
                ParameterSchema = micheParameter.ToBytes(),
                StorageSchema = micheStorage.ToBytes(),
                CodeSchema = micheCode.ToBytes(),
                Views = micheViews.Any()
                    ? micheViews.Select(x => x.ToBytes()).ToArray()
                    : null,
                Current = true
            };

            var viewsBytes = script.Views?
                .OrderBy(x => x, new BytesComparer())
                .SelectMany(x => x)
                .ToArray()
                ?? Array.Empty<byte>();
            var typeSchema = script.ParameterSchema.Concat(script.StorageSchema).Concat(viewsBytes);
            var fullSchema = typeSchema.Concat(script.CodeSchema);
            contract.TypeHash = script.TypeHash = Script.GetHash(typeSchema);
            contract.CodeHash = script.CodeHash = Script.GetHash(fullSchema);

            if (script.Schema.IsFA1())
            {
                if (script.Schema.IsFA12())
                    contract.Tags |= ContractTags.FA12;

                contract.Tags |= ContractTags.FA1;
                contract.Kind = ContractKind.Asset;
            }
            if (script.Schema.IsFA2())
            {
                contract.Tags |= ContractTags.FA2;
                contract.Kind = ContractKind.Asset;
            }

            Db.Scripts.Add(script);
            #endregion

            #region storage
            var storageValue = Micheline.FromJson(rawContract.Required("script").Required("storage"));
            var storage = new Storage
            {
                Id = Cache.AppState.NextStorageId(),
                Level = block.Level,
                ContractId = contract.Id,
                RawValue = script.Schema.OptimizeStorage(storageValue, false).ToBytes(),
                JsonValue = script.Schema.HumanizeStorage(storageValue),
                Current = true
            };

            Db.Storages.Add(storage);
            #endregion

            #region migration
            var migration = new MigrationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                Kind = MigrationKind.Origination,
                Account = contract,
                BalanceChange = contract.Balance,
                Script = script,
                Storage = storage,
            };

            script.MigrationId = migration.Id;
            storage.MigrationId = migration.Id;

            Db.TryAttach(block);
            block.Events |= BlockEvents.SmartContracts;
            block.Operations |= Operations.Migrations;

            var state = Cache.AppState.Get();
            Db.TryAttach(state);
            state.MigrationOpsCount++;

            var stats = Cache.Statistics.Current;
            Db.TryAttach(stats);
            stats.TotalCreated += contract.Balance;

            Db.MigrationOps.Add(migration);
            #endregion

            #region bigmaps
            var storageScript = new ContractStorage(micheStorage);
            var storageTree = storageScript.Schema.ToTreeView(storageValue);
            var bigmaps = storageTree.Nodes()
                .Where(x => x.Schema is BigMapSchema)
                .Select(x => (x, x.Schema as BigMapSchema, (int)(x.Value as MichelineInt).Value));

            foreach (var (bigmap, schema, ptr) in bigmaps)
            {
                block.Events |= BlockEvents.Bigmaps;

                migration.BigMapUpdates = (migration.BigMapUpdates ?? 0) + 1;
                Db.BigMapUpdates.Add(new BigMapUpdate
                {
                    Id = Cache.AppState.NextBigMapUpdateId(),
                    Action = BigMapAction.Allocate,
                    BigMapPtr = ptr,
                    Level = block.Level,
                    MigrationId = migration.Id
                });

                var allocated = new BigMap
                {
                    Id = Cache.AppState.NextBigMapId(),
                    Ptr = ptr,
                    ContractId = contract.Id,
                    StoragePath = bigmap.Path,
                    KeyType = schema.Key.ToMicheline().ToBytes(),
                    ValueType = schema.Value.ToMicheline().ToBytes(),
                    Active = true,
                    FirstLevel = block.Level,
                    LastLevel = block.Level,
                    ActiveKeys = 0,
                    TotalKeys = 0,
                    Updates = 1,
                    Tags = BigMaps.GetTags(contract, bigmap)
                };
                Db.BigMaps.Add(allocated);

                if (address == LiquidityToken && allocated.StoragePath == "tokens")
                {
                    var rawKey = new MichelineString(NullAddress.Address);
                    var rawValue = new MichelineInt(100);

                    allocated.Tags |= BigMapTag.Ledger1;
                    allocated.ActiveKeys++;
                    allocated.TotalKeys++;
                    allocated.Updates++;
                    var key = new BigMapKey
                    {
                        Id = Cache.AppState.NextBigMapKeyId(),
                        Active = true,
                        BigMapPtr = ptr,
                        FirstLevel = block.Level,
                        LastLevel = block.Level,
                        JsonKey = schema.Key.Humanize(rawKey),
                        JsonValue = schema.Value.Humanize(rawValue),
                        RawKey = schema.Key.Optimize(rawKey).ToBytes(),
                        RawValue = schema.Value.Optimize(rawValue).ToBytes(),
                        KeyHash = schema.GetKeyHash(rawKey),
                        Updates = 1
                    };
                    Db.BigMapKeys.Add(key);

                    migration.BigMapUpdates++;
                    Db.BigMapUpdates.Add(new BigMapUpdate
                    {
                        Id = Cache.AppState.NextBigMapUpdateId(),
                        Action = BigMapAction.AddKey,
                        BigMapKeyId = key.Id,
                        BigMapPtr = key.BigMapPtr,
                        JsonValue = key.JsonValue,
                        RawValue = key.RawValue,
                        Level = key.LastLevel,
                        MigrationId = migration.Id
                    });

                    #region tokens
                    var token = new Token
                    {
                        Id = Cache.AppState.NextSubId(migration),
                        Tags = TokenTags.Fa12,
                        BalancesCount = 1,
                        ContractId = contract.Id,
                        FirstMinterId = contract.Id,
                        FirstLevel = migration.Level,
                        HoldersCount = 1,
                        LastLevel = migration.Level,
                        TokenId = 0,
                        TotalBurned = 0,
                        TotalMinted = 100,
                        TotalSupply = 100,
                        TransfersCount = 1
                    };
                    var tokenBalance = new TokenBalance
                    {
                        Id = Cache.AppState.NextSubId(migration),
                        AccountId = NullAddress.Id,
                        Balance = 100,
                        FirstLevel = migration.Level,
                        LastLevel = migration.Level,
                        TokenId = token.Id,
                        ContractId = token.ContractId,
                        TransfersCount = 1
                    };
                    var tokenTransfer = new TokenTransfer
                    {
                        Id = Cache.AppState.NextSubId(migration),
                        Amount = 100,
                        Level = migration.Level,
                        MigrationId = migration.Id,
                        ToId = NullAddress.Id,
                        TokenId = token.Id,
                        ContractId = token.ContractId
                    };

                    Db.Tokens.Add(token);
                    Db.TokenBalances.Add(tokenBalance);
                    Db.TokenTransfers.Add(tokenTransfer);

                    migration.TokenTransfers = 1;

                    state.TokensCount++;
                    state.TokenBalancesCount++;
                    state.TokenTransfersCount++;

                    contract.TokensCount++;
                    contract.Creator.ActiveTokensCount++;
                    contract.Creator.TokenBalancesCount++;
                    contract.Creator.TokenTransfersCount++;
                    contract.Creator.LastLevel = tokenTransfer.Level;

                    block.Events |= BlockEvents.Tokens;
                    #endregion
                }
                else if (address == FallbackToken && allocated.StoragePath == "tokens")
                {
                    allocated.Tags |= BigMapTag.Ledger1;
                }
            }
            #endregion
        }

        async Task RemoveContract(string address)
        {
            var contract = await Cache.Accounts.GetAsync(address) as Contract;
            Db.TryAttach(contract);

            var bigmaps = await Db.BigMaps.AsNoTracking()
                .Where(x => x.ContractId == contract.Id)
                .ToListAsync();

            var state = Cache.AppState.Get();
            Db.TryAttach(state);
            state.MigrationOpsCount--;

            var creator = await Cache.Accounts.GetAsync(contract.CreatorId);
            Db.TryAttach(creator);
            creator.ContractsCount--;

            if (address == LiquidityToken)
            {
                var token = await Db.Tokens
                    .AsNoTracking()
                    .Where(x => x.ContractId == contract.Id)
                    .SingleAsync();

                await Db.Database.ExecuteSqlRawAsync(@"
                    DELETE FROM ""TokenTransfers"" WHERE ""TokenId"" = {0};
                    DELETE FROM ""TokenBalances"" WHERE ""TokenId"" = {0};
                    DELETE FROM ""Tokens"" WHERE ""Id"" = {0};",
                    token.Id);

                state.TokenTransfersCount--;
                state.TokenBalancesCount--;
                state.TokensCount--;

                contract.TokensCount--;

                creator.ActiveTokensCount--;
                creator.TokenBalancesCount--;
                creator.TokenTransfersCount--;
            }

            await Db.Database.ExecuteSqlRawAsync(@"
                DELETE FROM ""MigrationOps"" WHERE ""AccountId"" = {1};
                DELETE FROM ""Storages"" WHERE ""ContractId"" = {1};
                DELETE FROM ""Scripts"" WHERE ""ContractId"" = {1};
                DELETE FROM ""BigMapUpdates"" WHERE ""BigMapPtr"" = ANY({0});
                DELETE FROM ""BigMapKeys"" WHERE ""BigMapPtr"" = ANY({0});
                DELETE FROM ""BigMaps"" WHERE ""Ptr"" = ANY({0});",
                bigmaps.Select(x => x.Ptr).ToList(), contract.Id);

            Cache.AppState.ReleaseOperationId();
            Cache.AppState.ReleaseScriptId();
            Cache.AppState.ReleaseStorageId();
            Cache.Storages.Remove(contract);
            Cache.Schemas.Remove(contract);
            Cache.BigMapKeys.Reset();
            foreach (var bigmap in bigmaps)
            {
                Cache.BigMaps.Remove(bigmap);
                Cache.AppState.ReleaseBigMapId();
                Cache.AppState.ReleaseBigMapKeyId(bigmap.TotalKeys);
                Cache.AppState.ReleaseBigMapUpdateId(bigmap.Updates);
            }

            if (contract.TokenTransfersCount != 0)
            {
                var ghost = new Account
                {
                    Id = contract.Id,
                    Address = contract.Address,
                    FirstBlock = contract.FirstBlock,
                    FirstLevel = contract.FirstLevel,
                    LastLevel = contract.LastLevel,
                    ActiveTokensCount = contract.ActiveTokensCount,
                    TokenBalancesCount = contract.TokenBalancesCount,
                    TokenTransfersCount = contract.TokenTransfersCount,
                    Type = AccountType.Ghost,
                };

                Db.Entry(contract).State = EntityState.Detached;
                Db.Entry(ghost).State = EntityState.Modified;
                Cache.Accounts.Add(ghost);
            }
        }
    }
}
