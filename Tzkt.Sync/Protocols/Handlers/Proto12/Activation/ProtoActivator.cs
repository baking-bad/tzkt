﻿using System.Numerics;
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
            protocol.AttestersPerBlock = parameters["consensus_committee_size"]?.Value<int>() ?? 7000;
            protocol.MinimalStake = parameters["tokens_per_roll"]?.Value<long>() ?? 6_000_000_000;
            protocol.BlockDeposit = 0;
            protocol.AttestationDeposit = 0;

            var totalReward = 80_000_000 / (60 / protocol.TimeBetweenBlocks);
            protocol.BlockReward0 = parameters["baking_reward_fixed_portion"]?.Value<long>() ?? (totalReward / 4);
            protocol.BlockReward1 = parameters["baking_reward_bonus_per_slot"]?.Value<long>() ?? (totalReward / 4 / (protocol.AttestersPerBlock / 3));
            protocol.AttestationReward0 = parameters["endorsing_reward_per_slot"]?.Value<long>() ?? (totalReward / 2 / protocol.AttestersPerBlock);
            protocol.AttestationReward1 = 0;

            protocol.LBToggleThreshold = (parameters["liquidity_baking_escape_ema_threshold"]?.Value<int>() ?? 666_667) * 1000;

            protocol.ConsensusThreshold = parameters["consensus_threshold"]?.Value<int>() ?? 4667;
            protocol.MinParticipationNumerator = parameters["minimal_participation_ratio"]?["numerator"]?.Value<int>() ?? 2;
            protocol.MinParticipationDenominator = parameters["minimal_participation_ratio"]?["denominator"]?.Value<int>() ?? 3;
            protocol.DenunciationPeriod = 1;
            protocol.SlashingDelay = 1;
            protocol.MaxDelegatedOverFrozenRatio = 100 / (parameters["frozen_deposits_percentage"]?.Value<int>() ?? 10) - 1;

            protocol.MaxBakingReward = protocol.BlockReward0 + protocol.AttestersPerBlock / 3 * protocol.BlockReward1;
            protocol.MaxAttestationReward = protocol.AttestersPerBlock * protocol.AttestationReward0;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            protocol.AttestersPerBlock = 7000;
            protocol.MinimalStake = 6_000_000_000;
            protocol.BlockDeposit = 0;
            protocol.AttestationDeposit = 0;

            var totalReward = 80_000_000 / (60 / protocol.TimeBetweenBlocks);
            protocol.BlockReward0 = totalReward / 4;
            protocol.BlockReward1 = totalReward / 4 / (protocol.AttestersPerBlock / 3);
            protocol.AttestationReward0 = totalReward / 2 / protocol.AttestersPerBlock;
            protocol.AttestationReward1 = 0;

            protocol.LBToggleThreshold = 666_667_000;

            protocol.ConsensusThreshold = 4667;
            protocol.MinParticipationNumerator = 2;
            protocol.MinParticipationDenominator = 3;
            protocol.DenunciationPeriod = 1;
            protocol.SlashingDelay = 1;
            protocol.MaxDelegatedOverFrozenRatio = 9;

            protocol.MaxBakingReward = protocol.BlockReward0 + protocol.AttestersPerBlock / 3 * protocol.BlockReward1;
            protocol.MaxAttestationReward = protocol.AttestersPerBlock * protocol.AttestationReward0;
        }

        protected override async Task MigrateContext(AppState state)
        {
            var prevProto = await Cache.Protocols.GetAsync(state.Protocol);
            var nextProto = await Cache.Protocols.GetAsync(state.NextProtocol);

            var bakers = await MigrateBakers();
            await MigrateCycles(state, bakers, nextProto);
            MigrateStatistics(bakers, nextProto);

            if (state.Level == 1) return;

            Proto.Diagnostics.TrackChanges();
            await Db.SaveChangesAsync();

            await MigrateSnapshots(state);
            await MigrateCurrentRights(state, prevProto, nextProto);
            await MigrateFutureRights(state, nextProto);

            Cache.BakingRights.Reset();
            Cache.BakerCycles.Reset();
        }

        protected override Task RevertContext(AppState state)
        {
            throw new NotImplementedException("Reverting Ithaca migration block is technically impossible");
        }

        async Task<List<Data.Models.Delegate>> MigrateBakers()
        {            
            var bakers = await Db.Delegates.ToListAsync();
            foreach (var baker in bakers)
            {
                Cache.Accounts.Add(baker);
                baker.StakingBalance = baker.Balance + baker.DelegatedBalance;
            }
            return bakers.Where(x => x.Staked).ToList();
        }

        async Task MigrateCycles(AppState state, List<Data.Models.Delegate> bakers, Protocol nextProto)
        {
            var selectedStakes = bakers
                .Where(x => x.StakingBalance >= nextProto.MinimalStake)
                .Select(x => Math.Min(x.StakingBalance, x.Balance * (nextProto.MaxDelegatedOverFrozenRatio + 1)));

            var selectedBakers = selectedStakes.Count();
            var selectedStake = selectedStakes.Sum();

            foreach (var cycle in await Db.Cycles.Where(x => x.LastLevel > state.Level).ToListAsync())
            {
                cycle.SnapshotLevel = state.Level;
                cycle.TotalBakers = selectedBakers;
                cycle.TotalBakingPower = selectedStake;
            }
        }

        void MigrateStatistics(List<Data.Models.Delegate> bakers, Protocol nextProto)
        {
            var stats = Cache.Statistics.Current;
            Db.TryAttach(stats);
            stats.TotalFrozen = bakers
                .Where(x => x.Staked && x.StakingBalance >= nextProto.MinimalStake)
                .Sum(x => {
                    var activeStake = Math.Min(x.StakingBalance, x.Balance * (nextProto.MaxDelegatedOverFrozenRatio + 1));
                    return activeStake / (nextProto.MaxDelegatedOverFrozenRatio + 1);
                });
        }

        Task<int> MigrateSnapshots(AppState state)
        {
            return Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "SnapshotBalances"
                WHERE "Level" = {0};

                INSERT INTO "SnapshotBalances" (
                    "Level",
                    "AccountId",
                    "BakerId",
                    "OwnDelegatedBalance",
                    "ExternalDelegatedBalance",
                    "DelegatorsCount",
                    "OwnStakedBalance",
                    "ExternalStakedBalance",
                    "StakersCount"
                )
                SELECT
                    {0},
                    "Id",
                    COALESCE("DelegateId", "Id"),
                    COALESCE("StakingBalance", "Balance") - COALESCE("DelegatedBalance", 0),
                    COALESCE("DelegatedBalance", 0),
                    COALESCE("DelegatorsCount", 0),
                    0,
                    0,
                    0
                FROM "Accounts"
                WHERE "Staked" = true;
                """, state.Level);
        }

        async Task MigrateCurrentRights(AppState state, Protocol prevProto, Protocol nextProto)
        {
            var cycle = await Db.Cycles.AsNoTracking().FirstAsync(x => x.Index == state.Cycle);
            if (state.Level == cycle.LastLevel) return;

            var bakerCycles = await Cache.BakerCycles.GetAsync(state.Cycle);
            var selectedBakers = await Db.Delegates.AsNoTracking()
                .Where(x => x.Staked && x.StakingBalance >= nextProto.MinimalStake)
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

            foreach (var ar in rights.Where(x => x.Type == BakingRightType.Attestation))
            {
                var bakerCycle = bakerCycles[ar.BakerId];
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureAttestations -= ar.Slots!.Value;
                bakerCycle.FutureAttestationRewards -= GetFutureAttestationReward(prevProto, state.Cycle, ar.Slots.Value);
            }

            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "BakingRights"
                WHERE "Level" > {0} AND "Cycle" = {1}
                """, state.Level, state.Cycle);
                
            #endregion

            #region add missed baker cycles
            foreach (var baker in selectedBakers)
            {
                if (!bakerCycles.TryGetValue(baker.Id, out var bc))
                {
                    bc = new BakerCycle
                    {
                        Id = 0,
                        Cycle = state.Cycle,
                        BakerId = baker.Id
                    };
                    Db.BakerCycles.Add(bc);
                    Cache.BakerCycles.Add(bc);

                    if (baker.DelegatorsCount > 0)
                    {
                        await Db.Database.ExecuteSqlRawAsync("""
                            INSERT INTO "DelegatorCycles" (
                                "Cycle",
                                "DelegatorId",
                                "BakerId",
                                "DelegatedBalance",
                                "StakedBalance"
                            )
                            SELECT
                                {0},
                                "AccountId",
                                "BakerId",
                                "OwnDelegatedBalance",
                                "OwnStakedBalance"
                            FROM "SnapshotBalances"
                            WHERE "Level" = {1}
                            AND "AccountId" != "BakerId"
                            AND "BakerId" = {2}
                            """, state.Cycle, state.Level, baker.Id);
                    }
                }
            }
            #endregion

            #region update baker cycles
            foreach (var (bakerId, bc) in bakerCycles)
            {
                var baker = Cache.Accounts.GetDelegate(bakerId);

                Db.TryAttach(bc);
                bc.OwnDelegatedBalance = baker.StakingBalance - baker.DelegatedBalance;
                bc.ExternalDelegatedBalance = baker.DelegatedBalance;
                bc.DelegatorsCount = baker.DelegatorsCount;
                bc.OwnStakedBalance = baker.OwnStakedBalance;
                bc.ExternalStakedBalance = baker.ExternalStakedBalance;
                bc.StakersCount = baker.StakersCount;
                bc.BakingPower = 0;
                bc.TotalBakingPower = cycle.TotalBakingPower;

                if (baker.StakingBalance >= nextProto.MinimalStake)
                {
                    var bakingPower = Math.Min(baker.StakingBalance, baker.Balance * (nextProto.MaxDelegatedOverFrozenRatio + 1));
                    var expectedAttestations = (int)(new BigInteger(nextProto.BlocksPerCycle) * nextProto.AttestersPerBlock * bakingPower / cycle.TotalBakingPower);
                    bakerCycles[baker.Id].BakingPower = bakingPower;
                    bakerCycles[baker.Id].FutureAttestationRewards += expectedAttestations * nextProto.AttestationReward0;
                }
            }
            #endregion

            #region apply new rights
            var sampler = GetSampler(selectedBakers
                .Where(x => x.Balance > 0)
                .Select(x => (x.Id, Math.Min(x.StakingBalance, x.Balance * (nextProto.MaxDelegatedOverFrozenRatio + 1)))));

            #region temporary diagnostics
            await sampler.Validate(Proto, state.Level, cycle.Index);
            #endregion

            var brs = new List<RightsGenerator.BR>();
            var ars = new List<RightsGenerator.AR>();
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
                foreach (var ar in RightsGenerator.GetAttestationRights(sampler, nextProto, cycle, level - 1))
                {
                    ars.Add(ar);
                    var bakerCycle = bakerCycles[ar.Baker];
                    Db.TryAttach(bakerCycle);
                    bakerCycle.FutureAttestations += ar.Slots;
                }
            }

            var conn = (Db.Database.GetDbConnection() as NpgsqlConnection)!;
            using var writer = conn.BeginBinaryImport(@"
                COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"")
                FROM STDIN (FORMAT BINARY)");

            foreach (var ar in ars)
            {
                writer.StartRow();
                writer.Write(cycle.Index, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(ar.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(ar.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((int)BakingRightType.Attestation, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((int)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.WriteNull();
                writer.Write(ar.Slots, NpgsqlTypes.NpgsqlDbType.Integer);
            }

            foreach (var br in brs)
            {
                writer.StartRow();
                writer.Write(cycle.Index, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(br.Level, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(br.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((int)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((int)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(br.Round, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.WriteNull();
            }

            writer.Complete();
            #endregion
        }

        async Task MigrateFutureRights(AppState state, Protocol nextProto)
        {
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "BakingRights" WHERE "Cycle" > {0};
                DELETE FROM "BakerCycles" WHERE "Cycle" > {0};
                DELETE FROM "DelegatorCycles" WHERE "Cycle" > {0};
                """, state.Cycle);

            var bakers = await Db.Delegates.AsNoTracking().Where(x => x.Staked).ToListAsync();
            var sampler = GetSampler(bakers
                .Where(x => x.StakingBalance >= nextProto.MinimalStake && x.Balance > 0)
                .Select(x => (x.Id, Math.Min(x.StakingBalance, x.Balance * (nextProto.MaxDelegatedOverFrozenRatio + 1)))));

            #region temporary diagnostics
            await sampler.Validate(Proto, state.Level, state.Cycle);
            #endregion

            var cycles = await Db.Cycles.AsNoTracking().Where(x => x.Index >= state.Cycle).OrderBy(x => x.Index).ToListAsync();
            var conn = (Db.Database.GetDbConnection() as NpgsqlConnection)!;

            #region save shifted
            var currentCycle = cycles.First();
            var shifted = RightsGenerator.GetAttestationRights(sampler, nextProto, currentCycle, currentCycle.LastLevel);

            using (var writer = conn.BeginBinaryImport(@"
                COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"")
                FROM STDIN (FORMAT BINARY)"))
            {
                foreach (var ar in shifted)
                {
                    writer.StartRow();
                    writer.Write(currentCycle.Index + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(ar.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(ar.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((int)BakingRightType.Attestation, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((int)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.WriteNull();
                    writer.Write(ar.Slots, NpgsqlTypes.NpgsqlDbType.Integer);
                }

                writer.Complete();
            }
            #endregion
            
            foreach (var cycle in cycles.Skip(1))
            {
                GC.Collect();
                var brs = await RightsGenerator.GetBakingRightsAsync(sampler, nextProto, cycle);
                var ars = await RightsGenerator.GetAttestationRightsAsync(sampler, nextProto, cycle);

                #region save rights
                using (var writer = conn.BeginBinaryImport(@"
                    COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"")
                    FROM STDIN (FORMAT BINARY)"))
                {
                    foreach (var ar in ars)
                    {
                        writer.StartRow();
                        writer.Write(nextProto.GetCycle(ar.Level + 1), NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write(ar.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write(ar.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write((int)BakingRightType.Attestation, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write((int)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.WriteNull();
                        writer.Write(ar.Slots, NpgsqlTypes.NpgsqlDbType.Integer);
                    }

                    foreach (var br in brs)
                    {
                        writer.StartRow();
                        writer.Write(cycle.Index, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write(br.Level, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write(br.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write((int)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write((int)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write(br.Round, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.WriteNull();
                    }

                    writer.Complete();
                }
                #endregion

                #region save delegator cycles
                await Db.Database.ExecuteSqlRawAsync("""
                    INSERT INTO "DelegatorCycles" (
                        "Cycle",
                        "DelegatorId",
                        "BakerId",
                        "DelegatedBalance",
                        "StakedBalance"
                    )
                    SELECT
                        {0},
                        "AccountId",
                        "BakerId",
                        "OwnDelegatedBalance",
                        "OwnStakedBalance"
                    FROM "SnapshotBalances"
                    WHERE "Level" = {1}
                    AND "AccountId" != "BakerId"
                    """, cycle.Index, cycle.SnapshotLevel);
                #endregion

                #region save baker cycles
                var bakerCycles = bakers.ToDictionary(x => x.Id, x =>
                {
                    var bc = new BakerCycle
                    {
                        Id = 0,
                        BakerId = x.Id,
                        Cycle = cycle.Index,
                        OwnDelegatedBalance = x.StakingBalance - x.DelegatedBalance,
                        ExternalDelegatedBalance = x.DelegatedBalance,
                        DelegatorsCount = x.DelegatorsCount,
                        OwnStakedBalance = x.OwnStakedBalance,
                        ExternalStakedBalance = x.ExternalStakedBalance,
                        StakersCount = x.StakersCount,
                        BakingPower = 0,
                        TotalBakingPower = cycle.TotalBakingPower
                    };
                    if (x.StakingBalance >= nextProto.MinimalStake && x.Balance > 0)
                    {
                        var bakingPower = Math.Min(x.StakingBalance, x.Balance * (nextProto.MaxDelegatedOverFrozenRatio + 1));
                        var expectedAttestations = (int)(new BigInteger(nextProto.BlocksPerCycle) * nextProto.AttestersPerBlock * bakingPower / cycle.TotalBakingPower);
                        bc.BakingPower = bakingPower;
                        bc.ExpectedBlocks = nextProto.BlocksPerCycle * bakingPower / cycle.TotalBakingPower;
                        bc.ExpectedAttestations = expectedAttestations;
                        bc.FutureAttestationRewards = expectedAttestations * nextProto.AttestationReward0;
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

                #region apply future attestation rights
                foreach (var ar in shifted)
                {
                    if (bakerCycles.TryGetValue(ar.Baker, out var bakerCycle))
                    {
                        bakerCycle.FutureAttestations += ar.Slots;
                    }
                }
                foreach (var ar in ars.TakeWhile(x => x.Level < cycle.LastLevel))
                {
                    if (!bakerCycles.TryGetValue(ar.Baker, out var bakerCycle))
                        throw new Exception("Nonexistent baker cycle");

                    bakerCycle.FutureAttestations += ar.Slots;
                }
                #endregion

                shifted = ars.Where(x => x.Level == cycle.LastLevel).ToList();
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
