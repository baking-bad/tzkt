using Microsoft.EntityFrameworkCore;
using System.Numerics;
using Netmavryk.Encoding;
using Newtonsoft.Json.Linq;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    partial class ProtoActivator : Proto18.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.ConsensusRightsDelay = parameters["consensus_rights_delay"]?.Value<int>() ?? 2;
            protocol.DelegateParametersActivationDelay = parameters["delegate_parameters_activation_delay"]?.Value<int>() ?? 5;
            protocol.DoubleBakingSlashedPercentage = parameters["percentage_of_frozen_deposits_slashed_per_double_baking"]?.Value<int>() ?? 500;
            protocol.DoubleEndorsingSlashedPercentage = parameters["percentage_of_frozen_deposits_slashed_per_double_attestation"]?.Value<int>() ?? 5000;
            protocol.BlocksPerSnapshot = protocol.BlocksPerCycle;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            if (protocol.ConsensusRightsDelay == 5)
                protocol.ConsensusRightsDelay = 2;

            if (protocol.TimeBetweenBlocks >= 8)
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
