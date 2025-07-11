using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using Netmavryk.Contracts;
using Netmavryk.Encoding;
using Newtonsoft.Json.Linq;
using Npgsql;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto20
{
    partial class ProtoActivator : Proto19.ProtoActivator
    {
        public const string ProtocolTreasuryContract = "KT1J1w34sDTh1dwjn9B7urJse9Dm53qKd9AM";
        
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            if (protocol.ConsensusRightsDelay == 5)
                protocol.ConsensusRightsDelay = 2;

            // if (protocol.TimeBetweenBlocks >= 8) TODO: reapply for mainnet
            if (protocol.TimeBetweenBlocks >= 5)
            {
                protocol.BlocksPerCycle = protocol.BlocksPerCycle * 3 / 2;
                protocol.BlocksPerCommitment = protocol.BlocksPerCommitment * 3 / 2;
                protocol.BlocksPerVoting = protocol.BlocksPerVoting * 3 / 2;
                protocol.TimeBetweenBlocks = protocol.TimeBetweenBlocks * 2 / 3;
                protocol.HardBlockGasLimit = prev.HardBlockGasLimit * 2 / 3;
                protocol.SmartRollupCommitmentPeriod = 15 * 60 / protocol.TimeBetweenBlocks;
                protocol.SmartRollupChallengeWindow = 14 * 24 * 60 * 60 / protocol.TimeBetweenBlocks;
                protocol.SmartRollupTimeoutPeriod = 7 * 24 * 60 * 60 / protocol.TimeBetweenBlocks;
            }

            protocol.BlocksPerSnapshot = protocol.BlocksPerCycle;
        }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.ConsensusRightsDelay = parameters["consensus_rights_delay"]?.Value<int>() ?? 2;
            protocol.DelegateParametersActivationDelay = parameters["delegate_parameters_activation_delay"]?.Value<int>() ?? 5;
            protocol.DoubleBakingSlashedPercentage = parameters["percentage_of_frozen_deposits_slashed_per_double_baking"]?.Value<int>() ?? 500;
            protocol.DoubleEndorsingSlashedPercentage = parameters["percentage_of_frozen_deposits_slashed_per_double_attestation"]?.Value<int>() ?? 5000;
            protocol.BlocksPerSnapshot = protocol.BlocksPerCycle;
        }

        protected override async Task MigrateContext(AppState state)
        {
            var prevProto = await Cache.Protocols.GetAsync(state.Protocol);
            var nextProto = await Cache.Protocols.GetAsync(state.NextProtocol);

            await RemoveDeadRefutationGames(state);
            await RemoveFutureCycles(state, prevProto, nextProto);
            MigrateBakers(state, prevProto, nextProto);
            await MigrateVotingPeriods(state, nextProto);
            var cycles = await MigrateCycles(state, nextProto);
            await MigrateFutureRights(state, nextProto, cycles);
        }

        async Task RemoveFutureCycles(AppState state, Protocol prevProto, Protocol nextProto)
        {
            if (prevProto.ConsensusRightsDelay == nextProto.ConsensusRightsDelay)
                return;

            var lastCycle = state.Cycle + nextProto.ConsensusRightsDelay + 1;
            var lastCycleStart = nextProto.GetCycleStart(lastCycle);
            
            await Db.Database.ExecuteSqlRawAsync($"""
                DELETE FROM "BakerCycles"
                WHERE "Cycle" > {lastCycle};
                """);

            await Db.Database.ExecuteSqlRawAsync($"""
                DELETE FROM "BakingRights"
                WHERE "Type" = {(int)BakingRightType.Baking}
                AND "Cycle" > {lastCycle};
                
                DELETE FROM "BakingRights"
                WHERE "Type" = {(int)BakingRightType.Endorsing}
                AND "Level" > {lastCycleStart};
                """);

            var removedCycles = await Db.Database.ExecuteSqlRawAsync($"""
                DELETE FROM "Cycles"
                WHERE "Index" > {lastCycle};
                """);

            await Db.Database.ExecuteSqlRawAsync($"""
                DELETE FROM "DelegatorCycles"
                WHERE "Cycle" > {lastCycle};
                """);

            Cache.BakerCycles.Reset();
            Cache.BakingRights.Reset();

            Db.TryAttach(state);
            state.CyclesCount -= removedCycles;
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

        void MigrateBakers(AppState state, Protocol prevProto, Protocol nextProto)
        {
            foreach (var baker in Cache.Accounts.GetDelegates().Where(x => x.DeactivationLevel > state.Level))
            {
                Db.TryAttach(baker);
                baker.DeactivationLevel = nextProto.GetCycleStart(prevProto.GetCycle(baker.DeactivationLevel));
            }
        }

        async Task MigrateVotingPeriods(AppState state, Protocol nextProto)
        {
            var newPeriod = await Cache.Periods.GetAsync(state.VotingPeriod);
            Db.TryAttach(newPeriod);
            newPeriod.LastLevel = newPeriod.FirstLevel + nextProto.BlocksPerVoting - 1;
        }

        async Task<List<Cycle>> MigrateCycles(AppState state, Protocol nextProto)
        {
            var cycles = await Db.Cycles
                .Where(x => x.Index >= state.Cycle)
                .OrderBy(x => x.Index)
                .ToListAsync();

            var res = await Proto.Rpc.GetExpectedIssuance(state.Level);
            var issuance = res.EnumerateArray().First(x => x.RequiredInt32("cycle") == cycles.First(x => x.Index > state.Cycle).Index);

            foreach (var cycle in cycles.Where(x => x.Index > state.Cycle))
            {
                cycle.BlockReward = issuance.RequiredInt64("baking_reward_fixed_portion");
                cycle.BlockBonusPerSlot = issuance.RequiredInt64("baking_reward_bonus_per_slot");
                cycle.MaxBlockReward = cycle.BlockReward + cycle.BlockBonusPerSlot * (nextProto.EndorsersPerBlock - nextProto.ConsensusThreshold);
                cycle.EndorsementRewardPerSlot = issuance.RequiredInt64("attesting_reward_per_slot");
                cycle.NonceRevelationReward = issuance.RequiredInt64("seed_nonce_revelation_tip");
                cycle.VdfRevelationReward = issuance.RequiredInt64("vdf_revelation_tip");

                cycle.FirstLevel = nextProto.GetCycleStart(cycle.Index);
                cycle.LastLevel = nextProto.GetCycleEnd(cycle.Index);
            }

            return cycles;
        }

        async Task MigrateFutureRights(AppState state, Protocol nextProto, List<Cycle> cycles)
        {
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "BakingRights"
                WHERE "Cycle" > {0}
                """, state.Cycle);

            var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
            IEnumerable<RightsGenerator.ER> shifted = Enumerable.Empty<RightsGenerator.ER>();

            foreach (var cycle in cycles)
            {
                var bakerCycles = await Cache.BakerCycles.GetAsync(cycle.Index);
                var sampler = GetSampler(bakerCycles.Values
                    .Where(x => x.BakingPower > 0)
                    .Select(x => (x.BakerId, x.BakingPower))
                    .ToList());

                #region temporary diagnostics
                await sampler.Validate(Proto, state.Level, cycle.Index);
                #endregion

                if (cycle.Index == state.Cycle)
                {
                    shifted = RightsGenerator.GetEndorsingRights(sampler, nextProto, cycle, cycle.LastLevel);

                    #region save shifted
                    using var writer = conn.BeginBinaryImport("""
                        COPY "BakingRights" ("Cycle", "Level", "BakerId", "Type", "Status", "Round", "Slots")
                        FROM STDIN (FORMAT BINARY)
                        """);

                    foreach (var er in shifted)
                    {
                        writer.StartRow();
                        writer.Write(cycle.Index + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write(er.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write(er.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write((byte)BakingRightType.Endorsing, NpgsqlTypes.NpgsqlDbType.Smallint);
                        writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                        writer.WriteNull();
                        writer.Write(er.Slots, NpgsqlTypes.NpgsqlDbType.Integer);
                    }

                    writer.Complete();
                    #endregion
                }
                else
                {
                    GC.Collect();
                    var brs = await RightsGenerator.GetBakingRightsAsync(sampler, nextProto, cycle);
                    var ers = await RightsGenerator.GetEndorsingRightsAsync(sampler, nextProto, cycle);

                    #region save rights
                    using (var writer = conn.BeginBinaryImport("""
                        COPY "BakingRights" ("Cycle", "Level", "BakerId", "Type", "Status", "Round", "Slots")
                        FROM STDIN (FORMAT BINARY)
                        """))
                    {
                        foreach (var er in ers)
                        {
                            writer.StartRow();
                            writer.Write(nextProto.GetCycle(er.Level + 1), NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.Write(er.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.Write(er.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.Write((byte)BakingRightType.Endorsing, NpgsqlTypes.NpgsqlDbType.Smallint);
                            writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                            writer.WriteNull();
                            writer.Write(er.Slots, NpgsqlTypes.NpgsqlDbType.Integer);
                        }

                        foreach (var br in brs)
                        {
                            writer.StartRow();
                            writer.Write(cycle.Index, NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.Write(br.Level, NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.Write(br.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.Write((byte)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Smallint);
                            writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                            writer.Write(br.Round, NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.WriteNull();
                        }

                        writer.Complete();
                    }
                    #endregion

                    #region reset baker cycles
                    foreach (var bakerCycle in bakerCycles.Values)
                    {
                        Db.TryAttach(bakerCycle);

                        bakerCycle.FutureBlocks = 0;
                        bakerCycle.FutureBlockRewards = 0;
                        bakerCycle.FutureEndorsements = 0;

                        var expectedEndorsements = (int)(new BigInteger(nextProto.BlocksPerCycle) * nextProto.EndorsersPerBlock * bakerCycle.BakingPower / cycle.TotalBakingPower);
                        bakerCycle.ExpectedBlocks = nextProto.BlocksPerCycle * bakerCycle.BakingPower / cycle.TotalBakingPower;
                        bakerCycle.ExpectedEndorsements = expectedEndorsements;
                        bakerCycle.FutureEndorsementRewards = expectedEndorsements * cycle.EndorsementRewardPerSlot;
                    }

                    foreach (var br in brs.Where(x => x.Round == 0))
                    {
                        if (!bakerCycles.TryGetValue(br.Baker, out var bakerCycle))
                            throw new Exception("Nonexistent baker cycle");

                        bakerCycle.FutureBlocks++;
                        bakerCycle.FutureBlockRewards += cycle.MaxBlockReward;
                    }

                    foreach (var er in shifted)
                    {
                        if (bakerCycles.TryGetValue(er.Baker, out var bakerCycle))
                        {
                            bakerCycle.FutureEndorsements += er.Slots;
                        }
                    }

                    foreach (var er in ers.TakeWhile(x => x.Level < cycle.LastLevel))
                    {
                        if (!bakerCycles.TryGetValue(er.Baker, out var bakerCycle))
                            throw new Exception("Nonexistent baker cycle");

                        bakerCycle.FutureEndorsements += er.Slots;
                    }
                    #endregion

                    shifted = ers.Where(x => x.Level == cycle.LastLevel).ToList();
                }
            }
        }

        protected override Sampler GetSampler(IEnumerable<(int id, long stake)> selection)
        {
            var sorted = selection.OrderByDescending(x =>
            {
                var baker = Cache.Accounts.GetDelegate(x.id);
                return new byte[] { (byte)baker.PublicKey[0] }.Concat(Base58.Parse(baker.Address));
            }, new BytesComparer());

            return new Sampler(sorted.Select(x => x.id).ToArray(), sorted.Select(x => x.stake).ToArray());
        }
    }
}
