using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
using Newtonsoft.Json.Linq;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    partial class ProtoActivator : Proto11.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.BlocksPerSnapshot = parameters["blocks_per_stake_snapshot"]?.Value<int>() ?? 512;
            protocol.EndorsersPerBlock = parameters["consensus_committee_size"]?.Value<int>() ?? 7000;
            protocol.TokensPerRoll = parameters["tokens_per_roll"]?.Value<long>() ?? 6_000_000_000;
            protocol.BlockDeposit = 0;
            protocol.EndorsementDeposit = 0;

            var totalReward = 80_000_000 / (60 / protocol.TimeBetweenBlocks);
            protocol.BlockReward0 = parameters["baking_reward_fixed_portion"]?.Value<long>() ?? (totalReward / 4);
            protocol.BlockReward1 = parameters["baking_reward_bonus_per_slot"]?.Value<long>() ?? (totalReward / 4 / (protocol.EndorsersPerBlock / 3));
            protocol.EndorsementReward0 = parameters["endorsing_reward_per_slot"]?.Value<long>() ?? (totalReward / 2 / protocol.EndorsersPerBlock);
            protocol.EndorsementReward1 = 0;

            protocol.LBToggleThreshold = (parameters["liquidity_baking_escape_ema_threshold"]?.Value<int>() ?? 666_667) * 1000;

            protocol.ConsensusThreshold = parameters["consensus_threshold"]?.Value<int>() ?? 4667;
            protocol.MinParticipationNumerator = parameters["minimal_participation_ratio"]?["numerator"]?.Value<int>() ?? 2;
            protocol.MinParticipationDenominator = parameters["minimal_participation_ratio"]?["denominator"]?.Value<int>() ?? 3;
            protocol.MaxSlashingPeriod = parameters["max_slashing_period"]?.Value<int>() ?? 2;
            protocol.FrozenDepositsPercentage = parameters["frozen_deposits_percentage"]?.Value<int>() ?? 10;
            protocol.DoubleBakingPunishment = parameters["double_baking_punishment"]?.Value<long>() ?? 640_000_000;
            protocol.DoubleEndorsingPunishmentNumerator = parameters["ratio_of_frozen_deposits_slashed_per_double_endorsement"]?["numerator"]?.Value<int>() ?? 1;
            protocol.DoubleEndorsingPunishmentDenominator = parameters["ratio_of_frozen_deposits_slashed_per_double_endorsement"]?["denominator"]?.Value<int>() ?? 2;

            protocol.MaxBakingReward = protocol.BlockReward0 + protocol.EndorsersPerBlock / 3 * protocol.BlockReward1;
            protocol.MaxEndorsingReward = protocol.EndorsersPerBlock * protocol.EndorsementReward0;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            protocol.EndorsersPerBlock = 7000;
            protocol.TokensPerRoll = 6_000_000_000;
            protocol.BlockDeposit = 0;
            protocol.EndorsementDeposit = 0;

            var totalReward = 80_000_000 / (60 / protocol.TimeBetweenBlocks);
            protocol.BlockReward0 = totalReward / 4;
            protocol.BlockReward1 = totalReward / 4 / (protocol.EndorsersPerBlock / 3);
            protocol.EndorsementReward0 = totalReward / 2 / protocol.EndorsersPerBlock;
            protocol.EndorsementReward1 = 0;

            protocol.LBToggleThreshold = 666_667_000;

            protocol.ConsensusThreshold = 4667;
            protocol.MinParticipationNumerator = 2;
            protocol.MinParticipationDenominator = 3;
            protocol.MaxSlashingPeriod = 2;
            protocol.FrozenDepositsPercentage = 10;
            protocol.DoubleBakingPunishment = 640_000_000;
            protocol.DoubleEndorsingPunishmentNumerator = 1;
            protocol.DoubleEndorsingPunishmentDenominator = 2;

            protocol.MaxBakingReward = protocol.BlockReward0 + protocol.EndorsersPerBlock / 3 * protocol.BlockReward1;
            protocol.MaxEndorsingReward = protocol.EndorsersPerBlock * protocol.EndorsementReward0;
        }

        protected override async Task MigrateContext(AppState state)
        {
            var nextProto = await Cache.Protocols.GetAsync(state.NextProtocol);

            var bakers = await MigrateBakers(nextProto);
            await MigrateCycles(state, bakers, nextProto);
            await MigrateStatistics(state, bakers);
        }

        public async Task PostActivation(AppState state)
        {
            if (state.Level == 1) return;
            var prevProto = await Cache.Protocols.GetAsync(state.Protocol);
            var nextProto = await Cache.Protocols.GetAsync(state.NextProtocol);

            await MigrateSnapshots(state);
            await MigrateCurrentRights(state, prevProto, nextProto);
            await MigrateFutureRights(state, nextProto);

            Cache.BakingRights.Reset();
            Cache.BakerCycles.Reset();

            await Db.SaveChangesAsync();
        }

        public Task PreDeactivation(AppState state)
        {
            throw new NotImplementedException("Reverting Ithaca migration block is technically impossible");
        }

        protected override Task RevertContext(AppState state)
        {
            throw new NotImplementedException("Reverting Ithaca migration block is technically impossible");
        }

        async Task<List<Data.Models.Delegate>> MigrateBakers(Protocol nextProto)
        {            
            var bakers = await Db.Delegates.ToListAsync();
            foreach (var baker in bakers)
            {
                Cache.Accounts.Add(baker);
                baker.StakingBalance = baker.Balance + baker.DelegatedBalance;
                if (baker.Staked && baker.StakingBalance >= nextProto.TokensPerRoll)
                {
                    var activeStake = Math.Min(baker.StakingBalance, baker.Balance * 100 / nextProto.FrozenDepositsPercentage);
                    baker.FrozenDeposit = activeStake * nextProto.FrozenDepositsPercentage / 100;
                }
                else
                {
                    baker.FrozenDeposit = 0;
                }
            }
            return bakers.Where(x => x.Staked).ToList();
        }

        async Task MigrateCycles(AppState state, List<Data.Models.Delegate> bakers, Protocol nextProto)
        {
            var selectedStakes = bakers
                .Where(x => x.StakingBalance >= nextProto.TokensPerRoll)
                .Select(x => Math.Min(x.StakingBalance, x.Balance * 100 / nextProto.FrozenDepositsPercentage));

            var totalStaking = bakers.Sum(x => x.StakingBalance);
            var totalDelegated = bakers.Sum(x => x.DelegatedBalance);
            var totalDelegators = bakers.Sum(x => x.DelegatorsCount);
            var totalBakers = bakers.Count;
            var selectedBakers = selectedStakes.Count();
            var selectedStake = selectedStakes.Sum();

            foreach (var cycle in await Db.Cycles.Where(x => x.LastLevel > state.Level).ToListAsync())
            {
                cycle.SnapshotIndex = 0;
                cycle.SnapshotLevel = state.Level;
                cycle.TotalStaking = totalStaking;
                cycle.TotalDelegated = totalDelegated;
                cycle.TotalDelegators = totalDelegators;
                cycle.TotalBakers = totalBakers;
                cycle.SelectedBakers = selectedBakers;
                cycle.SelectedStake = selectedStake;
            }
        }

        async Task MigrateStatistics(AppState state, List<Data.Models.Delegate> bakers)
        {
            var stats = await Cache.Statistics.GetAsync(state.Level);
            Db.TryAttach(stats);
            stats.TotalFrozen = bakers.Sum(x => x.FrozenDeposit);
        }

        async Task MigrateSnapshots(AppState state)
        {
            await Db.Database.ExecuteSqlRawAsync($@"
                DELETE FROM ""SnapshotBalances"" WHERE ""Level"" = {state.Level};
                INSERT INTO ""SnapshotBalances"" (""Level"", ""Balance"", ""AccountId"", ""DelegateId"", ""DelegatorsCount"", ""DelegatedBalance"", ""StakingBalance"")
                    SELECT {state.Level}, ""Balance"", ""Id"", ""DelegateId"", ""DelegatorsCount"", ""DelegatedBalance"", ""StakingBalance""
                    FROM ""Accounts""
                    WHERE ""Staked"" = true;");
        }

        async Task MigrateCurrentRights(AppState state, Protocol prevProto, Protocol nextProto)
        {
            var cycle = await Db.Cycles.AsNoTracking().FirstAsync(x => x.Index == state.Cycle);
            if (state.Level == cycle.LastLevel) return;

            var bakerCycles = await Cache.BakerCycles.GetAsync(state.Cycle);
            var selectedBakers = await Db.Delegates.AsNoTracking()
                .Where(x => x.Staked && x.StakingBalance >= nextProto.TokensPerRoll)
                .ToListAsync();

            #region revert current rights
            var rights = await Db.BakingRights
                .AsNoTracking()
                .Where(x => x.Level > state.Level && x.Cycle == state.Cycle)
                .ToListAsync();

            foreach (var br in rights.Where(x => x.Type == BakingRightType.Baking && x.Round == 0))
            {
                var bakerCycle = bakerCycles[br.BakerId];
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureBlocks--;
                bakerCycle.FutureBlockRewards -= GetFutureBlockReward(prevProto, state.Cycle);
            }

            foreach (var er in rights.Where(x => x.Type == BakingRightType.Endorsing))
            {
                var bakerCycle = bakerCycles[er.BakerId];
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureEndorsements -= (int)er.Slots;
                bakerCycle.FutureEndorsementRewards -= GetFutureEndorsementReward(prevProto, state.Cycle, (int)er.Slots);
            }

            await Db.Database.ExecuteSqlRawAsync($@"DELETE FROM ""BakingRights"" WHERE ""Level"" > {state.Level} AND ""Cycle"" = {state.Cycle}");
            #endregion

            #region add missed baker cycles
            foreach (var baker in selectedBakers)
            {
                if (!bakerCycles.TryGetValue(baker.Id, out var bc))
                {
                    bc = new()
                    {
                        BakerId = baker.Id,
                        Cycle = state.Cycle
                    };
                    Db.BakerCycles.Add(bc);
                    Cache.BakerCycles.Add(bc);

                    if (baker.DelegatorsCount > 0)
                    {
                        await Db.Database.ExecuteSqlRawAsync($@"
                            INSERT  INTO ""DelegatorCycles"" (""Cycle"", ""DelegatorId"", ""BakerId"", ""Balance"")
                            SELECT  {state.Cycle}, ""AccountId"", ""DelegateId"", ""Balance""
                            FROM    ""SnapshotBalances""
                            WHERE   ""Level"" = {state.Level}
                            AND     ""DelegateId"" = {baker.Id}");
                    }
                }
            }
            #endregion

            #region update baker cycles
            foreach (var (bakerId, bc) in bakerCycles)
            {
                var baker = Cache.Accounts.GetDelegate(bakerId);

                Db.TryAttach(bc);
                bc.DelegatedBalance = baker.DelegatedBalance;
                bc.DelegatorsCount = baker.DelegatorsCount;
                bc.StakingBalance = baker.StakingBalance;
                bc.ActiveStake = 0;
                bc.SelectedStake = cycle.SelectedStake;

                if (baker.StakingBalance >= nextProto.TokensPerRoll)
                {
                    var activeStake = Math.Min(baker.StakingBalance, baker.Balance * 100 / nextProto.FrozenDepositsPercentage);
                    var expectedEndorsements = (int)(new BigInteger(nextProto.BlocksPerCycle) * nextProto.EndorsersPerBlock * activeStake / cycle.SelectedStake);
                    bakerCycles[baker.Id].FutureEndorsementRewards += expectedEndorsements * nextProto.EndorsementReward0;
                    bakerCycles[baker.Id].ActiveStake = activeStake;
                }
            }
            #endregion

            #region apply new rights
            var sampler = GetSampler(selectedBakers
                .Where(x => x.Balance > 0)
                .Select(x => (x.Id, Math.Min(x.StakingBalance, x.Balance * 100 / nextProto.FrozenDepositsPercentage))));

            #region temporary diagnostics
            await sampler.Validate(Proto, state.Level, cycle.Index);
            #endregion

            var brs = new List<RightsGenerator.BR>();
            var ers = new List<RightsGenerator.ER>();
            for (int level = state.Level + 1; level <= cycle.LastLevel; level++)
            {
                foreach (var br in RightsGenerator.GetBakingRights(sampler, cycle, level))
                {
                    brs.Add(br);
                    if (br.Round == 0)
                    {
                        var bakerCycle = bakerCycles[br.Baker];
                        Db.TryAttach(bakerCycle);
                        bakerCycle.FutureBlocks++;
                        bakerCycle.FutureBlockRewards += nextProto.MaxBakingReward;
                    }
                }
                foreach (var er in RightsGenerator.GetEndorsingRights(sampler, nextProto, cycle, level - 1))
                {
                    ers.Add(er);
                    var bakerCycle = bakerCycles[er.Baker];
                    Db.TryAttach(bakerCycle);
                    bakerCycle.FutureEndorsements += er.Slots;
                }
            }

            var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
            using var writer = conn.BeginBinaryImport(@"
                COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"")
                FROM STDIN (FORMAT BINARY)");

            foreach (var er in ers)
            {
                writer.StartRow();
                writer.Write(cycle.Index, NpgsqlTypes.NpgsqlDbType.Integer);
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
            #endregion
        }

        async Task MigrateFutureRights(AppState state, Protocol nextProto)
        {
            await Db.Database.ExecuteSqlRawAsync($@"DELETE FROM ""BakingRights"" WHERE ""Cycle"" > {state.Cycle}");
            await Db.Database.ExecuteSqlRawAsync($@"DELETE FROM ""BakerCycles"" WHERE ""Cycle"" > {state.Cycle}");
            await Db.Database.ExecuteSqlRawAsync($@"DELETE FROM ""DelegatorCycles"" WHERE ""Cycle"" > {state.Cycle}");

            var bakers = await Db.Delegates.AsNoTracking().Where(x => x.Staked).ToListAsync();
            var sampler = GetSampler(bakers
                .Where(x => x.StakingBalance >= nextProto.TokensPerRoll && x.Balance > 0)
                .Select(x => (x.Id, Math.Min(x.StakingBalance, x.Balance * 100 / nextProto.FrozenDepositsPercentage))));

            #region temporary diagnostics
            await sampler.Validate(Proto, state.Level, state.Cycle);
            #endregion

            var cycles = await Db.Cycles.AsNoTracking().Where(x => x.Index >= state.Cycle).OrderBy(x => x.Index).ToListAsync();
            var conn = Db.Database.GetDbConnection() as NpgsqlConnection;

            #region save shifted
            var currentCycle = cycles.First();
            var shifted = RightsGenerator.GetEndorsingRights(sampler, nextProto, currentCycle, currentCycle.LastLevel);

            using (var writer = conn.BeginBinaryImport(@"
                COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"")
                FROM STDIN (FORMAT BINARY)"))
            {
                foreach (var er in shifted)
                {
                    writer.StartRow();
                    writer.Write(currentCycle.Index + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(er.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(er.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((byte)BakingRightType.Endorsing, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.WriteNull();
                    writer.Write(er.Slots, NpgsqlTypes.NpgsqlDbType.Integer);
                }

                writer.Complete();
            }
            #endregion
            
            foreach (var cycle in cycles.Skip(1))
            {
                GC.Collect();
                var brs = await RightsGenerator.GetBakingRightsAsync(sampler, nextProto, cycle);
                var ers = await RightsGenerator.GetEndorsingRightsAsync(sampler, nextProto, cycle);

                #region save rights
                using (var writer = conn.BeginBinaryImport(@"
                    COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"")
                    FROM STDIN (FORMAT BINARY)"))
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

                #region save delegator cycles
                await Db.Database.ExecuteSqlRawAsync($@"
                    INSERT  INTO ""DelegatorCycles"" (""Cycle"", ""DelegatorId"", ""BakerId"", ""Balance"")
                    SELECT  {cycle.Index}, ""AccountId"", ""DelegateId"", ""Balance""
                    FROM    ""SnapshotBalances""
                    WHERE   ""Level"" = {cycle.SnapshotLevel}
                    AND     ""DelegateId"" IS NOT NULL");
                #endregion

                #region save baker cycles
                var bakerCycles = bakers.ToDictionary(x => x.Id, x =>
                {
                    var bc = new BakerCycle
                    {
                        BakerId = x.Id,
                        Cycle = cycle.Index,
                        DelegatedBalance = x.DelegatedBalance,
                        DelegatorsCount = x.DelegatorsCount, 
                        StakingBalance = x.StakingBalance,
                        ActiveStake = 0,
                        SelectedStake = cycle.SelectedStake
                    };
                    if (x.StakingBalance >= nextProto.TokensPerRoll && x.Balance > 0)
                    {
                        var activeStake = Math.Min(x.StakingBalance, x.Balance * 100 / nextProto.FrozenDepositsPercentage);
                        var expectedEndorsements = (int)(new BigInteger(nextProto.BlocksPerCycle) * nextProto.EndorsersPerBlock * activeStake / cycle.SelectedStake);
                        bc.ExpectedBlocks = nextProto.BlocksPerCycle * activeStake / cycle.SelectedStake;
                        bc.ExpectedEndorsements = expectedEndorsements;
                        bc.FutureEndorsementRewards = expectedEndorsements * nextProto.EndorsementReward0;
                        bc.ActiveStake = activeStake;
                    }
                    return bc;
                });
                Db.BakerCycles.AddRange(bakerCycles.Values);
                #endregion

                #region apply future baking rights
                foreach (var br in brs.Where(x => x.Round == 0))
                {
                    if (!bakerCycles.TryGetValue(br.Baker, out var bakerCycle))
                        throw new Exception("Nonexistent baker cycle");

                    bakerCycle.FutureBlocks++;
                    bakerCycle.FutureBlockRewards += nextProto.MaxBakingReward;
                }
                #endregion

                #region apply future endorsing rights
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

        protected virtual Sampler GetSampler(IEnumerable<(int id, long stake)> selection)
        {
            var sorted = selection
                .OrderByDescending(x => x.stake)
                .ThenByDescending(x => Base58.Parse(Cache.Accounts.GetDelegate(x.id).Address), new BytesComparer());

            return new Sampler(sorted.Select(x => x.id).ToArray(), sorted.Select(x => x.stake).ToArray());
        }
    }
}
