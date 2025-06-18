using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;
using Netezos.Encoding;
using Newtonsoft.Json.Linq;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto10
{
    class ProtoActivator(ProtocolHandler proto) : Proto9.ProtoActivator(proto)
    {
        public const string CpmmContract = "KT1TxqZ8QtKvLu3V3JH7Gx58n7Co8pgtpQU5";
        public const string LiquidityToken = "KT1AafHA1C1vk959wvHWBispY9Y2f3fxBUUo";
        public const string FallbackToken = "KT1VqarPDicMFn1ejmQqqshUkUXTCTXwmkCN";
        public const string Tzbtc = "KT1PWx2mnDueood7fEmfbBDKx1D9BAnnXitn";

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            #region unchanged
            protocol.RampUpCycles = parameters["security_deposit_ramp_up_cycles"]?.Value<int>() ?? 0;
            protocol.NoRewardCycles = parameters["no_reward_cycles"]?.Value<int>() ?? 0;
            protocol.ByteCost = parameters["cost_per_byte"]?.Value<int>() ?? 250;
            protocol.HardOperationGasLimit = parameters["hard_gas_limit_per_operation"]?.Value<int>() ?? 1_040_000;
            protocol.HardOperationStorageLimit = parameters["hard_storage_limit_per_operation"]?.Value<int>() ?? 60_000;
            protocol.OriginationSize = parameters["origination_size"]?.Value<int>() ?? 257;
            protocol.ConsensusRightsDelay = parameters["preserved_cycles"]?.Value<int>() ?? 5;
            protocol.ToleratedInactivityPeriod = protocol.ConsensusRightsDelay + 1;
            protocol.MinimalStake = parameters["tokens_per_roll"]?.Value<long>() ?? 8_000_000_000;
            protocol.BallotQuorumMin = parameters["quorum_min"]?.Value<int>() ?? 2000;
            protocol.BallotQuorumMax = parameters["quorum_max"]?.Value<int>() ?? 7000;
            protocol.ProposalQuorum = parameters["min_proposal_quorum"]?.Value<int>() ?? 500;
            #endregion

            var br = parameters["baking_reward_per_endorsement"] as JArray;
            var ar = parameters["endorsement_reward"] as JArray;

            protocol.BlockDeposit = parameters["block_security_deposit"]?.Value<long>() ?? 640_000_000;
            protocol.AttestationDeposit = parameters["endorsement_security_deposit"]?.Value<long>() ?? 2_500_000;
            protocol.BlockReward0 = br == null ? 78_125 : br.Count > 0 ? br[0].Value<long>() : 0;
            protocol.BlockReward1 = br == null ? 11_719 : br.Count > 1 ? br[1].Value<long>() : protocol.BlockReward0;
            protocol.AttestationReward0 = ar == null ? 78_125 : ar.Count > 0 ? ar[0].Value<long>() : 0;
            protocol.AttestationReward1 = ar == null ? 52_083 : ar.Count > 1 ? ar[1].Value<long>() : protocol.AttestationReward0;

            protocol.BlocksPerCycle = parameters["blocks_per_cycle"]?.Value<int>() ?? 8192;
            protocol.BlocksPerCommitment = parameters["blocks_per_commitment"]?.Value<int>() ?? 64;
            protocol.BlocksPerSnapshot = parameters["blocks_per_roll_snapshot"]?.Value<int>() ?? 512;
            protocol.BlocksPerVoting = parameters["blocks_per_voting_period"]?.Value<int>() ?? 40960;

            protocol.AttestersPerBlock = parameters["endorsers_per_block"]?.Value<int>() ?? 256;
            protocol.HardBlockGasLimit = parameters["hard_gas_limit_per_block"]?.Value<int>() ?? 5_200_000;
            protocol.TimeBetweenBlocks = parameters["minimal_block_delay"]?.Value<int>() ?? 30;

            protocol.LBToggleThreshold = (parameters["liquidity_baking_escape_ema_threshold"]?.Value<int>() ?? 1_000_000) * 1000;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            protocol.BlockDeposit = 640_000_000;
            protocol.AttestationDeposit = 2_500_000;
            protocol.BlockReward0 = 78_125;
            protocol.BlockReward1 = 11_719;
            protocol.AttestationReward0 = 78_125;
            protocol.AttestationReward1 = 52_083;

            protocol.BlocksPerCycle *= 2;
            protocol.BlocksPerCommitment *= 2;
            protocol.BlocksPerSnapshot *= 2;
            protocol.BlocksPerVoting *= 2;

            protocol.AttestersPerBlock = 256;
            protocol.HardBlockGasLimit = 5_200_000;
            protocol.TimeBetweenBlocks /= 2;

            protocol.LBToggleThreshold = 1_000_000_000;
        }

        protected override async Task ActivateContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            await OriginateContract(block, CpmmContract);
            await OriginateContract(block, LiquidityToken);
            if (!await Cache.Accounts.ExistsAsync(Tzbtc))
                await OriginateContract(block, FallbackToken);
        }

        protected override async Task DeactivateContext(AppState state)
        {
            state.TokensCount--;
            state.TokenBalancesCount--;
            state.TokenTransfersCount--;

            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "BigMapUpdates";
                DELETE FROM "BigMapKeys";
                DELETE FROM "BigMaps";
                DELETE FROM "Tokens";
                DELETE FROM "TokenBalances";
                DELETE FROM "TokenTransfers";
                """);
            Cache.BigMapKeys.Reset();
            Cache.BigMaps.Reset();
            Cache.Tokens.Reset();
            Cache.TokenBalances.Reset();

            Cache.AppState.Get().BigMapCounter = 0;
            Cache.AppState.Get().BigMapKeyCounter = 0;
            Cache.AppState.Get().BigMapUpdateCounter = 0;
        }

        protected override async Task MigrateContext(AppState state)
        {
            var prevProto = await Cache.Protocols.GetAsync(state.Protocol);
            var nextProto = await Cache.Protocols.GetAsync(state.NextProtocol);

            #region update voting period
            var newPeriod = await Cache.Periods.GetAsync(state.VotingPeriod);
            Db.TryAttach(newPeriod);
            newPeriod.LastLevel = newPeriod.FirstLevel + nextProto.BlocksPerVoting; // - 1 + 1
            #endregion

            var cycles = await MigrateCycles(state, nextProto);
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "BakingRights"
                WHERE "Cycle" > {0}
                """, state.Cycle);
            await MigrateCurrentRights(state, prevProto, nextProto, state.Level);
            await MigrateFutureRights(cycles, state, nextProto, state.Level);
            MigrateDelegates(state, prevProto, nextProto);

            Cache.BakingRights.Reset();
            Cache.BakerCycles.Reset();
            Cache.Periods.Reset();

            var block = await Cache.Blocks.CurrentAsync();
            await OriginateContract(block, CpmmContract);
            await OriginateContract(block, LiquidityToken);
            if (!await Cache.Accounts.ExistsAsync(Tzbtc))
                await OriginateContract(block, FallbackToken);
        }

        protected override async Task RevertContext(AppState state)
        {
            var prevProto = await Cache.Protocols.GetAsync(state.Protocol);
            var nextProto = await Cache.Protocols.GetAsync(state.NextProtocol);

            #region update voting periods
            var newPeriod = await Cache.Periods.GetAsync(state.VotingPeriod);
            Db.TryAttach(newPeriod);
            newPeriod.LastLevel = newPeriod.FirstLevel + prevProto.BlocksPerVoting - 1;
            #endregion

            var cycles = await MigrateCycles(state, prevProto);
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "BakingRights"
                WHERE "Cycle" > {0}
                """, state.Cycle);
            await MigrateCurrentRights(state, nextProto, prevProto, state.Level - 1);
            await MigrateFutureRights(cycles, state, prevProto, state.Level - 1);
            MigrateDelegates(state, nextProto, prevProto);

            Cache.BakingRights.Reset();
            Cache.BakerCycles.Reset();
            Cache.Periods.Reset();

            await RemoveContract(CpmmContract);
            await RemoveContract(LiquidityToken);
            if (await Cache.Accounts.ExistsAsync(FallbackToken))
                await RemoveContract(FallbackToken);
        }

        async Task<List<Cycle>> MigrateCycles(AppState state, Protocol nextProto)
        {
            var cycles = await Db.Cycles
                .Where(x => x.Index > state.Cycle)
                .OrderBy(x => x.Index)
                .ToListAsync();

            foreach (var cycle in cycles)
            {
                cycle.FirstLevel = nextProto.GetCycleStart(cycle.Index);
                cycle.LastLevel = nextProto.GetCycleEnd(cycle.Index);
            }

            return cycles;
        }

        async Task MigrateCurrentRights(AppState state, Protocol prevProto, Protocol nextProto, int block)
        {
            var rights = await Db.BakingRights
                .AsNoTracking()
                .Where(x => x.Level > state.Level && x.Cycle == state.Cycle)
                .ToListAsync();

            foreach (var br in rights.Where(x => x.Type == BakingRightType.Baking && x.Round == 0))
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(state.Cycle, br.BakerId);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureBlockRewards -= GetFutureBlockReward(prevProto, state.Cycle);
                bakerCycle.FutureBlockRewards += GetFutureBlockReward(nextProto, state.Cycle);
            }

            foreach (var ar in rights.Where(x => x.Type == BakingRightType.Attestation))
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(state.Cycle, ar.BakerId);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureAttestationRewards -= GetFutureAttestationReward(prevProto, state.Cycle, ar.Slots!.Value);
                bakerCycle.FutureAttestations -= ar.Slots.Value;
            }

            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "BakingRights"
                WHERE "Level" > {0}
                AND "Type" = {1}
                """, state.Level, (int)BakingRightType.Attestation);

            var newArs = new List<BakingRight>();
            for (int level = state.Level + 1; level < nextProto.GetCycleStart(state.Cycle + 1); level++)
            {
                foreach (var ar in (await Proto.Rpc.GetLevelAttestationRightsAsync(block, level - 1)).EnumerateArray())
                {
                    newArs.Add(new BakingRight
                    {
                        Id = 0,
                        Type = BakingRightType.Attestation,
                        Status = BakingRightStatus.Future,
                        BakerId = Cache.Accounts.GetExistingDelegate(ar.RequiredString("delegate")).Id,
                        Cycle = state.Cycle,
                        Level = level,
                        Slots = ar.RequiredArray("slots").Count()
                    });
                }
            }

#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
            await Db.Database.ExecuteSqlRawAsync($"""
                INSERT INTO "BakingRights" ("Cycle", "Level", "BakerId", "Type", "Status", "Slots") VALUES
                {string.Join(',', newArs.Select(ar => $"({ar.Cycle},{ar.Level},{ar.BakerId},{(int)ar.Type},{(int)ar.Status},{ar.Slots})"))}
                """);
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.

            foreach (var ar in newArs)
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(state.Cycle, ar.BakerId);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureAttestationRewards += GetFutureAttestationReward(nextProto, state.Cycle, ar.Slots!.Value);
                bakerCycle.FutureAttestations += ar.Slots.Value;
            }
        }

        async Task MigrateFutureRights(List<Cycle> cycles, AppState state, Protocol nextProto, int block)
        {
            var nextCycle = state.Cycle + 1;
            var nextCycleStart = nextProto.GetCycleStart(nextCycle);
            var shiftedRights = (await Proto.Rpc.GetLevelAttestationRightsAsync(block, nextCycleStart - 1))
                .EnumerateArray()
                .ToList();

#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
            await Db.Database.ExecuteSqlRawAsync($"""
                INSERT INTO "BakingRights" ("Cycle", "Level", "BakerId", "Type", "Status", "Slots") VALUES
                {string.Join(',', shiftedRights.Select(ar => $@"(
                    {nextCycle},
                    {nextCycleStart},
                    {Cache.Accounts.GetExistingDelegate(ar.RequiredString("delegate")).Id},
                    {(int)BakingRightType.Attestation},
                    {(int)BakingRightStatus.Future},
                    {ar.RequiredArray("slots").Count()}
                )"))}
                """);
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.

            foreach (var cycle in cycles)
            {
                var bakerCycles = (await Db.BakerCycles.Where(x => x.Cycle == cycle.Index).ToListAsync())
                    .ToDictionary(x => x.BakerId);

                foreach (var bc in bakerCycles.Values)
                {
                    var share = (double)bc.BakingPower / cycle.TotalBakingPower;
                    bc.ExpectedBlocks = nextProto.BlocksPerCycle * share;
                    bc.ExpectedAttestations = nextProto.AttestersPerBlock * nextProto.BlocksPerCycle * share;
                    bc.FutureBlockRewards = 0;
                    bc.FutureBlocks = 0;
                    bc.FutureAttestationRewards = 0;
                    bc.FutureAttestations = 0;
                }

                await FetchBakingRights(nextProto, block, cycle, bakerCycles);
                shiftedRights = await FetchAttestationRights(nextProto, block, cycle, bakerCycles, shiftedRights);
            }
        }

        async Task FetchBakingRights(Protocol protocol, int block, Cycle cycle, Dictionary<int, BakerCycle> bakerCycles)
        {
            GC.Collect();
            var rights = (await Proto.Rpc.GetBakingRightsAsync(block, cycle.Index)).RequiredArray().EnumerateArray();
            if (!rights.Any() || rights.Count(x => x.RequiredInt32("priority") == 0) != protocol.BlocksPerCycle)
                throw new ValidationException("Rpc returned less baking rights (with priority 0) than it should be");

            var conn = (Db.Database.GetDbConnection() as NpgsqlConnection)!;
            using var writer = conn.BeginBinaryImport(@"COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"") FROM STDIN (FORMAT BINARY)");

            foreach (var br in rights)
            {
                var bakerId = Cache.Accounts.GetExistingDelegate(br.RequiredString("delegate")).Id;
                var round = br.RequiredInt32("priority");
                if (round == 0)
                {
                    var bakerCycle = bakerCycles[bakerId];
                    bakerCycle.FutureBlockRewards += GetFutureBlockReward(protocol, cycle.Index);
                    bakerCycle.FutureBlocks++;
                }

                writer.StartRow();
                writer.Write(cycle.Index, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(br.RequiredInt32("level"), NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(bakerId, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((int)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((int)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(round, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.WriteNull();
            }

            writer.Complete();
        }

        async Task<List<JsonElement>> FetchAttestationRights(Protocol protocol, int block, Cycle cycle, Dictionary<int, BakerCycle> bakerCycles, List<JsonElement> shiftedRights)
        {
            GC.Collect();
            var rights = (await Proto.Rpc.GetAttestationRightsAsync(block, cycle.Index)).RequiredArray().EnumerateArray();
            //var rights = new List<JsonElement>(protocol.BlocksPerCycle * protocol.AttestersPerBlock / 2);
            //var attempts = 0;

            //for (int level = cycle.FirstLevel; level <= cycle.LastLevel; level++)
            //{
            //    try
            //    {
            //        rights.AddRange((await Proto.Rpc.GetLevelAttestationRightsAsync(block, level)).RequiredArray().EnumerateArray());
            //        attempts = 0;
            //    }
            //    catch (Exception ex)
            //    {
            //        Logger.LogError(ex, "Failed to fetch attestation rights for level {level}", level);
            //        if (++attempts >= 10) throw new Exception("Too many RPC errors when fetching attestation rights");
            //        await Task.Delay(3000);
            //        level--;
            //    }
            //}

            if (!rights.Any() || rights.Sum(x => x.RequiredArray("slots").Count()) != protocol.BlocksPerCycle * protocol.AttestersPerBlock)
                throw new ValidationException("Rpc returned less attestation rights (slots) than it should be");

            #region save rights
            var conn = (Db.Database.GetDbConnection() as NpgsqlConnection)!;
            using var writer = conn.BeginBinaryImport(@"COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"") FROM STDIN (FORMAT BINARY)");

            foreach (var ar in rights)
            {
                writer.StartRow();
                writer.Write(protocol.GetCycle(ar.RequiredInt32("level") + 1), NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(ar.RequiredInt32("level") + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(Cache.Accounts.GetExistingDelegate(ar.RequiredString("delegate")).Id, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((int)BakingRightType.Attestation, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((int)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.WriteNull();
                writer.Write(ar.RequiredArray("slots").Count(), NpgsqlTypes.NpgsqlDbType.Integer);
            }

            writer.Complete();
            #endregion

            foreach (var ar in rights.Where(x => x.RequiredInt32("level") != cycle.LastLevel))
            {
                var baker = Cache.Accounts.GetExistingDelegate(ar.RequiredString("delegate"));
                var slots = ar.RequiredArray("slots").Count();

                if (!bakerCycles.TryGetValue(baker.Id, out var bakerCycle))
                    throw new Exception("Nonexistent baker cycle");

                bakerCycle.FutureAttestationRewards += GetFutureAttestationReward(protocol, cycle.Index, slots);
                bakerCycle.FutureAttestations += slots;
            }

            foreach (var ar in shiftedRights)
            {
                var baker = Cache.Accounts.GetExistingDelegate(ar.RequiredString("delegate"));
                var slots = ar.RequiredArray("slots").Count();

                if (!bakerCycles.TryGetValue(baker.Id, out var bakerCycle))
                {
                    #region shifting hack
                    var snapshottedBaker = await Proto.Rpc.GetDelegateAsync(cycle.SnapshotLevel, baker.Address);
                    var delegators = snapshottedBaker
                        .RequiredArray("delegated_contracts")
                        .EnumerateArray()
                        .Select(x => x.RequiredString())
                        .Where(x => x != baker.Address);

                    var stakingBalance = snapshottedBaker.RequiredInt64("staking_balance");
                    var delegatedBalance = snapshottedBaker.RequiredInt64("delegated_balance");

                    bakerCycle = new BakerCycle
                    {
                        Id = 0,
                        Cycle = cycle.Index,
                        BakerId = baker.Id,
                        OwnDelegatedBalance = stakingBalance - delegatedBalance,
                        ExternalDelegatedBalance = delegatedBalance,
                        DelegatorsCount = delegators.Count(),
                        OwnStakedBalance = 0,
                        ExternalStakedBalance = 0,
                        StakersCount = 0,
                        BakingPower = 0,
                        TotalBakingPower = cycle.TotalBakingPower,
                        ExpectedBlocks = 0,
                        ExpectedAttestations = 0
                    };
                    bakerCycles.Add(baker.Id, bakerCycle);
                    Db.BakerCycles.Add(bakerCycle);

                    foreach (var delegatorAddress in delegators)
                    {
                        var snapshottedDelegator = await Proto.Rpc.GetContractAsync(cycle.SnapshotLevel, delegatorAddress);
                        Db.DelegatorCycles.Add(new DelegatorCycle
                        {
                            Id = 0,
                            Cycle = cycle.Index,
                            DelegatorId = (await Cache.Accounts.GetExistingAsync(delegatorAddress)).Id,
                            BakerId = baker.Id,
                            DelegatedBalance = snapshottedDelegator.RequiredInt64("balance"),
                            StakedBalance = 0
                        });
                    }
                    #endregion
                }

                bakerCycle.FutureAttestationRewards += GetFutureAttestationReward(protocol, cycle.Index, slots);
                bakerCycle.FutureAttestations += slots;
            }

            return rights.Where(x => x.RequiredInt32("level") == cycle.LastLevel).ToList();
        }

        void MigrateDelegates(AppState state, Protocol prevProto, Protocol nextProto)
        {
            foreach (var delegat in Cache.Accounts.GetDelegates().Where(x => x.DeactivationLevel > state.Level))
            {
                Db.TryAttach(delegat);
                delegat.DeactivationLevel = nextProto.GetCycleStart(prevProto.GetCycle(delegat.DeactivationLevel));
            }
        }

        async Task OriginateContract(Block block, string address)
        {
            var rawContract = await Proto.Rpc.GetContractAsync(block.Level, address);

            #region contract
            Contract contract;
            var creator = await Cache.Accounts.GetExistingAsync(NullAddress.Address);
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
                    CreatorId = creator.Id,
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
                    CreatorId = creator.Id,
                    Type = AccountType.Contract,
                    Kind = ContractKind.SmartContract,
                    MigrationsCount = 1,
                };
                Db.Accounts.Add(contract);
            }
            Cache.Accounts.Add(contract);

            Db.TryAttach(creator);
            creator.ContractsCount++;
            #endregion

            #region script
            var code = (rawContract.Required("script").RequiredMicheline("code") as MichelineArray)!;
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
                ?? [];
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
            var storageValue = rawContract.Required("script").RequiredMicheline("storage");
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
                Level = block.Level,
                Timestamp = block.Timestamp,
                Kind = MigrationKind.Origination,
                AccountId = contract.Id,
                BalanceChange = contract.Balance,
                ScriptId = script.Id,
                StorageId = storage.Id,
            };

            script.MigrationId = migration.Id;
            storage.MigrationId = migration.Id;

            Db.TryAttach(block);
            block.Operations |= Operations.Migrations;

            var state = Cache.AppState.Get();
            Db.TryAttach(state);
            state.MigrationOpsCount++;

            var stats = Cache.Statistics.Current;
            Db.TryAttach(stats);
            stats.TotalCreated += contract.Balance;

            Db.MigrationOps.Add(migration);
            Context.MigrationOps.Add(migration);
            #endregion

            #region bigmaps
            var storageScript = new ContractStorage(micheStorage);
            var storageTree = storageScript.Schema.ToTreeView(storageValue);
            var bigmaps = storageTree.Nodes()
                .Where(x => x.Schema is BigMapSchema)
                .Select(x => (x, (x.Schema as BigMapSchema)!, (int)(x.Value as MichelineInt)!.Value));

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
                    creator.ActiveTokensCount++;
                    creator.TokenBalancesCount++;
                    creator.TokenTransfersCount++;
                    creator.LastLevel = tokenTransfer.Level;

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
            var contract = (await Cache.Accounts.GetExistingAsync(address) as Contract)!;
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

                await Db.Database.ExecuteSqlRawAsync("""
                    DELETE FROM "TokenTransfers" WHERE "TokenId" = {0};
                    DELETE FROM "TokenBalances" WHERE "TokenId" = {0};
                    DELETE FROM "Tokens" WHERE "Id" = {0};
                    """, token.Id);

                state.TokenTransfersCount--;
                state.TokenBalancesCount--;
                state.TokensCount--;

                contract.TokensCount--;

                creator.ActiveTokensCount--;
                creator.TokenBalancesCount--;
                creator.TokenTransfersCount--;
            }

            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "MigrationOps" WHERE "AccountId" = {0};
                DELETE FROM "Storages" WHERE "ContractId" = {0};
                DELETE FROM "Scripts" WHERE "ContractId" = {0};
                DELETE FROM "BigMapUpdates" WHERE "BigMapPtr" = ANY({1});
                DELETE FROM "BigMapKeys" WHERE "BigMapPtr" = ANY({1});
                DELETE FROM "BigMaps" WHERE "Ptr" = ANY({1});
                """, contract.Id, bigmaps.Select(x => x.Ptr).ToList());

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

        protected override long GetFutureBlockReward(Protocol protocol, int cycle)
            => cycle < protocol.NoRewardCycles ? 0 : (protocol.BlockReward0 * protocol.AttestersPerBlock);

        protected override long GetFutureAttestationReward(Protocol protocol, int cycle, int slots)
            => cycle < protocol.NoRewardCycles ? 0 : (slots * protocol.AttestationReward0);
    }
}
