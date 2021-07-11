using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto10
{
    class ProtoActivator : Proto9.ProtoActivator
    {
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
            protocol.BlockReward1 = br == null ? 11_719 : br.Count > 1 ? br[1].Value<long>() : 0;
            protocol.EndorsementReward0 = er == null ? 78_125 : er.Count > 0 ? er[0].Value<long>() : 0;
            protocol.EndorsementReward1 = er == null ? 52_083 : er.Count > 1 ? er[1].Value<long>() : 0;
            
            protocol.BlocksPerCycle = parameters["blocks_per_cycle"]?.Value<int>() ?? 8192;
            protocol.BlocksPerCommitment = parameters["blocks_per_commitment"]?.Value<int>() ?? 64;
            protocol.BlocksPerSnapshot = parameters["blocks_per_roll_snapshot"]?.Value<int>() ?? 512;
            protocol.BlocksPerVoting = parameters["blocks_per_voting_period"]?.Value<int>() ?? 40960;
            
            protocol.EndorsersPerBlock = parameters["endorsers_per_block"]?.Value<int>() ?? 256;
            protocol.HardBlockGasLimit = parameters["hard_gas_limit_per_block"]?.Value<int>() ?? 5_200_000;
            protocol.TimeBetweenBlocks = parameters["minimal_block_delay"]?.Value<int>() ?? 30;

            protocol.LBSubsidy = parameters["liquidity_baking_subsidy"]?.Value<int>() ?? 2_500_000;
            protocol.LBSunsetLevel = parameters["liquidity_baking_sunset_level"]?.Value<int>() ?? 2_032_928;
            protocol.LBEscapeThreshold = parameters["liquidity_baking_escape_ema_threshold"]?.Value<int>() ?? 1_000_000;
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
            protocol.LBEscapeThreshold = 1_000_000;
        }

        protected override async Task MigrateContext(AppState state)
        {
            var prevProto = await Cache.Protocols.GetAsync(state.Protocol);
            var nextProto = await Cache.Protocols.GetAsync(state.NextProtocol);

            #region update voting periods
            if (state.VotingPeriod > 1)
            {
                var prevPeriod = await Cache.Periods.GetAsync(state.VotingPeriod - 1);
                Db.TryAttach(prevPeriod);
                prevPeriod.LastLevel += 1;
            }

            var newPeriod = await Cache.Periods.GetAsync(state.VotingPeriod);
            Db.TryAttach(newPeriod); 
            newPeriod.FirstLevel += 1;
            newPeriod.LastLevel = newPeriod.FirstLevel + nextProto.BlocksPerVoting - 1;
            #endregion

            var cycles = await MigrateCycles(state, nextProto);
            await Db.Database.ExecuteSqlRawAsync($@"DELETE FROM ""BakingRights"" WHERE ""Cycle"" > {state.Cycle}");
            await MigrateCurrentRights(state, prevProto, nextProto, state.Level);
            await MigrateFutureRights(cycles, state, nextProto, state.Level);
            MigrateDelegates(state, prevProto, nextProto);

            Cache.BakingRights.Reset();
            Cache.BakerCycles.Reset();
            Cache.Periods.Reset();
        }

        protected override async Task RevertContext(AppState state)
        {
            var prevProto = await Cache.Protocols.GetAsync(state.Protocol);
            var nextProto = await Cache.Protocols.GetAsync(state.NextProtocol);

            #region update voting periods
            if (state.VotingPeriod > 1)
            {
                var prevPeriod = await Cache.Periods.GetAsync(state.VotingPeriod - 1);
                Db.TryAttach(prevPeriod);
                prevPeriod.LastLevel -= 1;
            }

            var newPeriod = await Cache.Periods.GetAsync(state.VotingPeriod);
            Db.TryAttach(newPeriod);
            newPeriod.FirstLevel -= 1;
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

            foreach (var br in rights.Where(x => x.Type == BakingRightType.Baking && x.Priority == 0))
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(state.Cycle, br.BakerId);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureBlockDeposits -= GetBlockDeposit(prevProto, state.Cycle);
                bakerCycle.FutureBlockDeposits += GetBlockDeposit(nextProto, state.Cycle);

                bakerCycle.FutureBlockRewards -= GetFutureBlockReward(prevProto, state.Cycle);
                bakerCycle.FutureBlockRewards += GetFutureBlockReward(nextProto, state.Cycle);
            }

            foreach (var er in rights.Where(x => x.Type == BakingRightType.Endorsing))
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(state.Cycle, er.BakerId);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureEndorsementDeposits -= GetEndorsementDeposit(prevProto, state.Cycle, (int)er.Slots);
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

                bakerCycle.FutureEndorsementDeposits += GetEndorsementDeposit(nextProto, state.Cycle, (int)er.Slots);
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
                    var rollsShare = (double)bc.Rolls / cycle.TotalRolls;
                    bc.ExpectedBlocks = nextProto.BlocksPerCycle * rollsShare;
                    bc.ExpectedEndorsements = nextProto.EndorsersPerBlock * nextProto.BlocksPerCycle * rollsShare;
                    bc.FutureBlockDeposits = 0;
                    bc.FutureBlockRewards = 0;
                    bc.FutureBlocks = 0;
                    bc.FutureEndorsementDeposits = 0;
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
            var rights = (await Proto.Rpc.GetBakingRightsAsync(block, cycle.Index)).EnumerateArray();

            var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
            using var writer = conn.BeginBinaryImport(@"COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Priority"", ""Slots"") FROM STDIN (FORMAT BINARY)");

            foreach (var br in rights)
            {
                var bakerId = Cache.Accounts.GetDelegate(br.RequiredString("delegate")).Id;
                var priority = br.RequiredInt32("priority");
                if (priority == 0)
                {
                    var bakerCycle = bakerCycles[bakerId];
                    bakerCycle.FutureBlockDeposits += GetBlockDeposit(protocol, cycle.Index);
                    bakerCycle.FutureBlockRewards += GetFutureBlockReward(protocol, cycle.Index);
                    bakerCycle.FutureBlocks++;
                }

                writer.StartRow();
                writer.Write(cycle.Index, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(br.RequiredInt32("level"), NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(bakerId, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((byte)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Smallint);
                writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                writer.Write(priority, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.WriteNull();
            }

            writer.Complete();
        }

        async Task<List<JsonElement>> FetchEndorsingRights(Protocol protocol, int block, Cycle cycle, Dictionary<int, BakerCycle> bakerCycles, List<JsonElement> shiftedRights)
        {
            GC.Collect();
            var rights = (await Proto.Rpc.GetEndorsingRightsAsync(block, cycle.Index)).EnumerateArray();

            #region save rights
            var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
            using var writer = conn.BeginBinaryImport(@"COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Priority"", ""Slots"") FROM STDIN (FORMAT BINARY)");

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

                bakerCycle.FutureEndorsementDeposits += GetEndorsementDeposit(protocol, cycle.Index, slots);
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
                    var snapshotedBaker = await Proto.Rpc.GetDelegateAsync(cycle.SnapshotLevel, baker.Address);
                    var delegators = snapshotedBaker
                        .RequiredArray("delegated_contracts")
                        .EnumerateArray()
                        .Select(x => x.RequiredString())
                        .Where(x => x != baker.Address);

                    var rolls = (int)(snapshotedBaker.RequiredInt64("staking_balance") / protocol.TokensPerRoll);
                    var rollsShare = (double)rolls / cycle.TotalRolls;

                    bakerCycle = new BakerCycle
                    {
                        Cycle = cycle.Index,
                        BakerId = baker.Id,
                        Rolls = rolls,
                        StakingBalance = snapshotedBaker.RequiredInt64("staking_balance"),
                        DelegatedBalance = snapshotedBaker.RequiredInt64("delegated_balance"),
                        DelegatorsCount = delegators.Count(),
                        ExpectedBlocks = protocol.BlocksPerCycle * rollsShare,
                        ExpectedEndorsements = protocol.EndorsersPerBlock * protocol.BlocksPerCycle * rollsShare
                    };
                    bakerCycles.Add(baker.Id, bakerCycle);
                    Db.BakerCycles.Add(bakerCycle);

                    foreach (var delegatorAddress in delegators)
                    {
                        var snapshotedDelegator = await Proto.Rpc.GetContractAsync(cycle.SnapshotLevel, delegatorAddress);
                        Db.DelegatorCycles.Add(new DelegatorCycle
                        {
                            BakerId = baker.Id,
                            Balance = snapshotedDelegator.RequiredInt64("balance"),
                            Cycle = cycle.Index,
                            DelegatorId = (await Cache.Accounts.GetAsync(delegatorAddress)).Id
                        });
                    }
                    #endregion
                }

                bakerCycle.FutureEndorsementDeposits += GetEndorsementDeposit(protocol, cycle.Index, slots);
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

        protected override long GetFutureBlockReward(Protocol protocol, int cycle)
            => cycle < protocol.NoRewardCycles ? 0 : (protocol.BlockReward0 * protocol.EndorsersPerBlock);

        protected override long GetBlockDeposit(Protocol protocol, int cycle)
            => cycle < protocol.RampUpCycles
                ? (protocol.BlockDeposit * cycle / protocol.RampUpCycles)
                : protocol.BlockDeposit;

        protected override long GetFutureEndorsementReward(Protocol protocol, int cycle, int slots)
            => cycle < protocol.NoRewardCycles ? 0 : (slots * protocol.EndorsementReward0);

        protected override long GetEndorsementDeposit(Protocol protocol, int cycle, int slots)
            => cycle < protocol.RampUpCycles
                ? (slots * protocol.EndorsementDeposit * cycle / protocol.RampUpCycles)
                : (slots * protocol.EndorsementDeposit);
    }
}
