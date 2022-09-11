using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;
using Netezos.Encoding;
using Newtonsoft.Json.Linq;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto10
{
    class ProtoActivator : Proto9.ProtoActivator
    {
        public const string CpmmContract = "KT1TxqZ8QtKvLu3V3JH7Gx58n7Co8pgtpQU5";
        public const string LiquidityToken = "KT1AafHA1C1vk959wvHWBispY9Y2f3fxBUUo";
        public const string FallbackToken = "KT1VqarPDicMFn1ejmQqqshUkUXTCTXwmkCN";
        public const string Tzbtc = "KT1PWx2mnDueood7fEmfbBDKx1D9BAnnXitn";

        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            #region unchanged
            protocol.RampUpCycles = parameters["security_deposit_ramp_up_cycles"]?.Value<int>() ?? 0;
            protocol.NoRewardCycles = parameters["no_reward_cycles"]?.Value<int>() ?? 0;
            protocol.ByteCost = parameters["cost_per_byte"]?.Value<int>() ?? 250;
            protocol.HardOperationGasLimit = parameters["hard_gas_limit_per_operation"]?.Value<int>() ?? 1_040_000;
            protocol.HardOperationStorageLimit = parameters["hard_storage_limit_per_operation"]?.Value<int>() ?? 60_000;
            protocol.OriginationSize = parameters["origination_size"]?.Value<int>() ?? 257;
            protocol.PreservedCycles = parameters["preserved_cycles"]?.Value<int>() ?? 5;
            protocol.RevelationReward = parameters["seed_nonce_revelation_tip"]?.Value<long>() ?? 125_000;
            protocol.TokensPerRoll = parameters["tokens_per_roll"]?.Value<long>() ?? 8_000_000_000;
            protocol.BallotQuorumMin = parameters["quorum_min"]?.Value<int>() ?? 2000;
            protocol.BallotQuorumMax = parameters["quorum_max"]?.Value<int>() ?? 7000;
            protocol.ProposalQuorum = parameters["min_proposal_quorum"]?.Value<int>() ?? 500;
            #endregion

            var br = parameters["baking_reward_per_endorsement"] as JArray;
            var er = parameters["endorsement_reward"] as JArray;

            protocol.BlockDeposit = parameters["block_security_deposit"]?.Value<long>() ?? 640_000_000;
            protocol.EndorsementDeposit = parameters["endorsement_security_deposit"]?.Value<long>() ?? 2_500_000;
            protocol.BlockReward0 = br == null ? 78_125 : br.Count > 0 ? br[0].Value<long>() : 0;
            protocol.BlockReward1 = br == null ? 11_719 : br.Count > 1 ? br[1].Value<long>() : protocol.BlockReward0;
            protocol.EndorsementReward0 = er == null ? 78_125 : er.Count > 0 ? er[0].Value<long>() : 0;
            protocol.EndorsementReward1 = er == null ? 52_083 : er.Count > 1 ? er[1].Value<long>() : protocol.EndorsementReward0;

            protocol.BlocksPerCycle = parameters["blocks_per_cycle"]?.Value<int>() ?? 8192;
            protocol.BlocksPerCommitment = parameters["blocks_per_commitment"]?.Value<int>() ?? 64;
            protocol.BlocksPerSnapshot = parameters["blocks_per_roll_snapshot"]?.Value<int>() ?? 512;
            protocol.BlocksPerVoting = parameters["blocks_per_voting_period"]?.Value<int>() ?? 40960;

            protocol.EndorsersPerBlock = parameters["endorsers_per_block"]?.Value<int>() ?? 256;
            protocol.HardBlockGasLimit = parameters["hard_gas_limit_per_block"]?.Value<int>() ?? 5_200_000;
            protocol.TimeBetweenBlocks = parameters["minimal_block_delay"]?.Value<int>() ?? 30;

            protocol.LBSubsidy = parameters["liquidity_baking_subsidy"]?.Value<int>() ?? 2_500_000;
            protocol.LBSunsetLevel = parameters["liquidity_baking_sunset_level"]?.Value<int>() ?? 2_032_928;
            protocol.LBToggleThreshold = (parameters["liquidity_baking_escape_ema_threshold"]?.Value<int>() ?? 1_000_000) * 1000;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            protocol.BlockDeposit = 640_000_000;
            protocol.EndorsementDeposit = 2_500_000;
            protocol.BlockReward0 = 78_125;
            protocol.BlockReward1 = 11_719;
            protocol.EndorsementReward0 = 78_125;
            protocol.EndorsementReward1 = 52_083;

            protocol.BlocksPerCycle *= 2;
            protocol.BlocksPerCommitment *= 2;
            protocol.BlocksPerSnapshot *= 2;
            protocol.BlocksPerVoting *= 2;

            protocol.EndorsersPerBlock = 256;
            protocol.HardBlockGasLimit = 5_200_000;
            protocol.TimeBetweenBlocks /= 2;

            protocol.LBSubsidy = 2_500_000;
            protocol.LBSunsetLevel = 2_032_928;
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

            await Db.Database.ExecuteSqlRawAsync(@"
                DELETE FROM ""BigMapUpdates"";
                DELETE FROM ""BigMapKeys"";
                DELETE FROM ""BigMaps"";
                DELETE FROM ""Tokens"";
                DELETE FROM ""TokenBalances"";
                DELETE FROM ""TokenTransfers"";");
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
            await Db.Database.ExecuteSqlRawAsync($@"DELETE FROM ""BakingRights"" WHERE ""Cycle"" > {state.Cycle}");
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
            await Db.Database.ExecuteSqlRawAsync($@"DELETE FROM ""BakingRights"" WHERE ""Cycle"" > {state.Cycle}");
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

            foreach (var er in rights.Where(x => x.Type == BakingRightType.Endorsing))
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(state.Cycle, er.BakerId);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureEndorsementRewards -= GetFutureEndorsementReward(prevProto, state.Cycle, (int)er.Slots);
                bakerCycle.FutureEndorsements -= (int)er.Slots;
            }

            await Db.Database.ExecuteSqlRawAsync($@"
                DELETE FROM ""BakingRights""
                WHERE ""Level"" > {state.Level}
                AND ""Type"" = {(int)BakingRightType.Endorsing}");

            var newErs = new List<BakingRight>();
            for (int level = state.Level + 1; level < nextProto.GetCycleStart(state.Cycle + 1); level++)
            {
                foreach (var er in (await Proto.Rpc.GetLevelEndorsingRightsAsync(block, level - 1)).EnumerateArray())
                {
                    newErs.Add(new BakingRight
                    {
                        Type = BakingRightType.Endorsing,
                        Status = BakingRightStatus.Future,
                        BakerId = Cache.Accounts.GetDelegate(er.RequiredString("delegate")).Id,
                        Cycle = state.Cycle,
                        Level = level,
                        Slots = er.RequiredArray("slots").Count()
                    });
                }
            }

            await Db.Database.ExecuteSqlRawAsync($@"
                INSERT INTO ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Slots"") VALUES
                {string.Join(',', newErs.Select(er => $"({er.Cycle},{er.Level},{er.BakerId},{(int)er.Type},{(int)er.Status},{er.Slots})"))}");

            foreach (var er in newErs)
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(state.Cycle, er.BakerId);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(nextProto, state.Cycle, (int)er.Slots);
                bakerCycle.FutureEndorsements += (int)er.Slots;
            }
        }

        async Task MigrateFutureRights(List<Cycle> cycles, AppState state, Protocol nextProto, int block)
        {
            var nextCycle = state.Cycle + 1;
            var nextCycleStart = nextProto.GetCycleStart(nextCycle);
            var shiftedRights = (await Proto.Rpc.GetLevelEndorsingRightsAsync(block, nextCycleStart - 1))
                .EnumerateArray()
                .ToList();

            await Db.Database.ExecuteSqlRawAsync($@"
                INSERT INTO ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Slots"") VALUES
                {string.Join(',', shiftedRights.Select(er => $@"(
                    {nextCycle},
                    {nextCycleStart},
                    {Cache.Accounts.GetDelegate(er.RequiredString("delegate")).Id},
                    {(byte)BakingRightType.Endorsing},
                    {(byte)BakingRightStatus.Future},
                    {er.RequiredArray("slots").Count()}
                )"))}");

            foreach (var cycle in cycles)
            {
                var bakerCycles = (await Db.BakerCycles.Where(x => x.Cycle == cycle.Index).ToListAsync())
                    .ToDictionary(x => x.BakerId);

                foreach (var bc in bakerCycles.Values)
                {
                    var share = (double)bc.StakingBalance / cycle.TotalStaking;
                    bc.ExpectedBlocks = nextProto.BlocksPerCycle * share;
                    bc.ExpectedEndorsements = nextProto.EndorsersPerBlock * nextProto.BlocksPerCycle * share;
                    bc.FutureBlockRewards = 0;
                    bc.FutureBlocks = 0;
                    bc.FutureEndorsementRewards = 0;
                    bc.FutureEndorsements = 0;
                }

                await FetchBakingRights(nextProto, block, cycle, bakerCycles);
                shiftedRights = await FetchEndorsingRights(nextProto, block, cycle, bakerCycles, shiftedRights);
            }
        }

        async Task FetchBakingRights(Protocol protocol, int block, Cycle cycle, Dictionary<int, BakerCycle> bakerCycles)
        {
            GC.Collect();
            var rights = (await Proto.Rpc.GetBakingRightsAsync(block, cycle.Index)).RequiredArray().EnumerateArray();
            if (!rights.Any() || rights.Count(x => x.RequiredInt32("priority") == 0) != protocol.BlocksPerCycle)
                throw new ValidationException("Rpc returned less baking rights (with priority 0) than it should be");

            var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
            using var writer = conn.BeginBinaryImport(@"COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"") FROM STDIN (FORMAT BINARY)");

            foreach (var br in rights)
            {
                var bakerId = Cache.Accounts.GetDelegate(br.RequiredString("delegate")).Id;
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
                writer.Write((byte)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Smallint);
                writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                writer.Write(round, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.WriteNull();
            }

            writer.Complete();
        }

        async Task<List<JsonElement>> FetchEndorsingRights(Protocol protocol, int block, Cycle cycle, Dictionary<int, BakerCycle> bakerCycles, List<JsonElement> shiftedRights)
        {
            GC.Collect();
            var rights = (await Proto.Rpc.GetEndorsingRightsAsync(block, cycle.Index)).RequiredArray().EnumerateArray();
            //var rights = new List<JsonElement>(protocol.BlocksPerCycle * protocol.EndorsersPerBlock / 2);
            //var attempts = 0;

            //for (int level = cycle.FirstLevel; level <= cycle.LastLevel; level++)
            //{
            //    try
            //    {
            //        rights.AddRange((await Proto.Rpc.GetLevelEndorsingRightsAsync(block, level)).RequiredArray().EnumerateArray());
            //        attempts = 0;
            //    }
            //    catch (Exception ex)
            //    {
            //        Logger.LogError("Failed to fetch endorsing rights for level {0}: {1}", level, ex.Message);
            //        if (++attempts >= 10) throw new Exception("Too many RPC errors when fetching endorsing rights");
            //        await Task.Delay(3000);
            //        level--;
            //    }
            //}

            if (!rights.Any() || rights.Sum(x => x.RequiredArray("slots").Count()) != protocol.BlocksPerCycle * protocol.EndorsersPerBlock)
                throw new ValidationException("Rpc returned less endorsing rights (slots) than it should be");

            #region save rights
            var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
            using var writer = conn.BeginBinaryImport(@"COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"") FROM STDIN (FORMAT BINARY)");

            foreach (var er in rights)
            {
                writer.StartRow();
                writer.Write(protocol.GetCycle(er.RequiredInt32("level") + 1), NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(er.RequiredInt32("level") + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(Cache.Accounts.GetDelegate(er.RequiredString("delegate")).Id, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((byte)BakingRightType.Endorsing, NpgsqlTypes.NpgsqlDbType.Smallint);
                writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                writer.WriteNull();
                writer.Write(er.RequiredArray("slots").Count(), NpgsqlTypes.NpgsqlDbType.Integer);
            }

            writer.Complete();
            #endregion

            foreach (var er in rights.Where(x => x.RequiredInt32("level") != cycle.LastLevel))
            {
                var baker = Cache.Accounts.GetDelegate(er.RequiredString("delegate"));
                var slots = er.RequiredArray("slots").Count();

                if (!bakerCycles.TryGetValue(baker.Id, out var bakerCycle))
                    throw new Exception("Nonexistent baker cycle");

                bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(protocol, cycle.Index, slots);
                bakerCycle.FutureEndorsements += slots;
            }

            foreach (var er in shiftedRights)
            {
                var baker = Cache.Accounts.GetDelegate(er.RequiredString("delegate"));
                var slots = er.RequiredArray("slots").Count();

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
                    var activeStake = stakingBalance - stakingBalance % protocol.TokensPerRoll;
                    var share = (double)activeStake / cycle.SelectedStake;

                    bakerCycle = new BakerCycle
                    {
                        Cycle = cycle.Index,
                        BakerId = baker.Id,
                        StakingBalance = stakingBalance,
                        ActiveStake = activeStake,
                        SelectedStake = cycle.SelectedStake,
                        DelegatedBalance = snapshottedBaker.RequiredInt64("delegated_balance"),
                        DelegatorsCount = delegators.Count(),
                        ExpectedBlocks = protocol.BlocksPerCycle * share,
                        ExpectedEndorsements = protocol.EndorsersPerBlock * protocol.BlocksPerCycle * share
                    };
                    bakerCycles.Add(baker.Id, bakerCycle);
                    Db.BakerCycles.Add(bakerCycle);

                    foreach (var delegatorAddress in delegators)
                    {
                        var snapshottedDelegator = await Proto.Rpc.GetContractAsync(cycle.SnapshotLevel, delegatorAddress);
                        Db.DelegatorCycles.Add(new DelegatorCycle
                        {
                            BakerId = baker.Id,
                            Balance = snapshottedDelegator.RequiredInt64("balance"),
                            Cycle = cycle.Index,
                            DelegatorId = (await Cache.Accounts.GetAsync(delegatorAddress)).Id
                        });
                    }
                    #endregion
                }

                bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(protocol, cycle.Index, slots);
                bakerCycle.FutureEndorsements += slots;
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

            block.Events |= BlockEvents.SmartContracts;
            block.Operations |= Operations.Migrations;

            var state = Cache.AppState.Get();
            state.MigrationOpsCount++;

            var statistics = await Cache.Statistics.GetAsync(state.Level);
            statistics.TotalCreated += contract.Balance;

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
            var bigmaps = await Db.BigMaps.AsNoTracking()
                .Where(x => x.ContractId == contract.Id)
                .ToListAsync();

            var state = Cache.AppState.Get();
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

        protected override long GetFutureBlockReward(Protocol protocol, int cycle)
            => cycle < protocol.NoRewardCycles ? 0 : (protocol.BlockReward0 * protocol.EndorsersPerBlock);

        protected override long GetFutureEndorsementReward(Protocol protocol, int cycle, int slots)
            => cycle < protocol.NoRewardCycles ? 0 : (slots * protocol.EndorsementReward0);
    }
}
