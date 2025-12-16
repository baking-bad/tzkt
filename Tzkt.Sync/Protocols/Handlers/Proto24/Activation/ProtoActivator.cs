using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto24
{
    partial class ProtoActivator(ProtocolHandler proto) : Proto23.ProtoActivator(proto)
    {
        protected override long GetBlockBonusPerBlock(JsonElement issuance, Protocol protocol)
            => issuance.RequiredInt64("baking_reward_bonus_per_block");

        protected override long GetAttestationRewardPerBlock(JsonElement issuance, Protocol protocol)
            => issuance.RequiredInt64("attesting_reward_per_block");

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            // mainnet
            if (protocol.BlocksPerCycle == 10_800 && protocol.TimeBetweenBlocks == 8)
            {
                protocol.BlocksPerCycle = protocol.BlocksPerCycle * 4 / 3;
                protocol.BlocksPerCommitment = protocol.BlocksPerCommitment * 4 / 3;
                protocol.BlocksPerSnapshot = protocol.BlocksPerCycle;
                protocol.BlocksPerVoting = protocol.BlocksPerVoting * 4 / 3;
                protocol.TimeBetweenBlocks = protocol.TimeBetweenBlocks * 3 / 4;
                protocol.HardBlockGasLimit = 1_040_000;

                protocol.SmartRollupCommitmentPeriod = 15 * 60 / protocol.TimeBetweenBlocks;
                protocol.SmartRollupChallengeWindow = 14 * 24 * 60 * 60 / protocol.TimeBetweenBlocks;
                protocol.SmartRollupTimeoutPeriod = 7 * 24 * 60 * 60 / protocol.TimeBetweenBlocks;
            }

            // shadownet
            if (Cache.AppState.Get().ChainId == "NetXsqzbfFenSTS")
            {
                protocol.BlocksPerVoting = protocol.BlocksPerCycle;
            }
        }

        protected override async Task MigrateContext(AppState state)
        {
            var prevProto = await Cache.Protocols.GetAsync(state.Protocol);
            var nextProto = await Cache.Protocols.GetAsync(state.NextProtocol);

            await InitAddressRegistry();
            await RemoveDeadRefutationGames(state);
            await MigrateSlashing(state, nextProto);
            MigrateBakers(state, prevProto, nextProto);
            await MigrateVotingPeriods(state, nextProto);
            var cycles = await MigrateCycles(state, nextProto);
            await MigrateFutureRights(state, nextProto, cycles);

            Cache.BakerCycles.Reset();
            Cache.BakingRights.Reset();
        }

        async Task InitAddressRegistry()
        {
            var nullAddress = await Cache.Accounts.GetAsync(NullAddress.Id);
            Db.TryAttach(nullAddress);
            nullAddress.Index = 0;
        }

        async Task MigrateSlashing(AppState state, Protocol nextProto)
        {
            foreach (var op in await Db.DoubleBakingOps.Where(x => x.SlashedLevel > state.Level).ToListAsync())
            {
                var proto = await Cache.Protocols.FindByLevelAsync(op.AccusedLevel);
                op.SlashedLevel = nextProto.GetCycleEnd(proto.GetCycle(op.AccusedLevel) + proto.SlashingDelay);
            }

            foreach (var op in await Db.DoubleConsensusOps.Where(x => x.SlashedLevel > state.Level).ToListAsync())
            {
                var proto = await Cache.Protocols.FindByLevelAsync(op.AccusedLevel);
                op.SlashedLevel = nextProto.GetCycleEnd(proto.GetCycle(op.AccusedLevel) + proto.SlashingDelay);
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

            foreach (var cycle in cycles.Where(x => x.Index > state.Cycle))
            {
                var issuance = res.EnumerateArray().First(x => x.RequiredInt32("cycle") == cycle.Index);

                cycle.BlockReward = issuance.RequiredInt64("baking_reward_fixed_portion");
                cycle.BlockBonusPerBlock = GetBlockBonusPerBlock(issuance, nextProto);
                cycle.AttestationRewardPerBlock = GetAttestationRewardPerBlock(issuance, nextProto);
                cycle.NonceRevelationReward = issuance.RequiredInt64("seed_nonce_revelation_tip");
                cycle.VdfRevelationReward = issuance.RequiredInt64("vdf_revelation_tip");
                cycle.DalAttestationRewardPerShard = issuance.RequiredInt64("dal_attesting_reward_per_shard");

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

            var conn = (Db.Database.GetDbConnection() as NpgsqlConnection)!;
            IEnumerable<RightsGenerator.AR> shifted = [];

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
                    shifted = RightsGenerator.GetAttestationRights(sampler, nextProto, cycle, cycle.LastLevel);

                    #region save shifted
                    using var writer = conn.BeginBinaryImport("""
                        COPY "BakingRights" ("Cycle", "Level", "BakerId", "Type", "Status", "Round", "Slots")
                        FROM STDIN (FORMAT BINARY)
                        """);

                    foreach (var ar in shifted)
                    {
                        writer.StartRow();
                        writer.Write(cycle.Index + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write(ar.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write(ar.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write((int)BakingRightType.Attestation, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write((int)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.WriteNull();
                        writer.Write(ar.Slots, NpgsqlTypes.NpgsqlDbType.Integer);
                    }

                    writer.Complete();
                    #endregion
                }
                else
                {
                    GC.Collect();
                    var brs = await RightsGenerator.GetBakingRightsAsync(sampler, nextProto, cycle);
                    var ars = await RightsGenerator.GetAttestationRightsAsync(sampler, nextProto, cycle);

                    #region save rights
                    using (var writer = conn.BeginBinaryImport("""
                        COPY "BakingRights" ("Cycle", "Level", "BakerId", "Type", "Status", "Round", "Slots")
                        FROM STDIN (FORMAT BINARY)
                        """))
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

                    #region reset baker cycles
                    var attestationRewardPerSlot = cycle.AttestationRewardPerBlock / nextProto.AttestersPerBlock;
                    var maxBlockReward = cycle.BlockReward + cycle.BlockBonusPerBlock;

                    foreach (var bakerCycle in bakerCycles.Values)
                    {
                        Db.TryAttach(bakerCycle);

                        bakerCycle.FutureBlocks = 0;
                        bakerCycle.FutureBlockRewards = 0;
                        bakerCycle.FutureAttestations = 0;

                        var expectedAttestations = (nextProto.BlocksPerCycle * nextProto.AttestersPerBlock).MulRatio(bakerCycle.BakingPower, cycle.TotalBakingPower);
                        bakerCycle.ExpectedBlocks = nextProto.BlocksPerCycle.MulRatio(bakerCycle.BakingPower, cycle.TotalBakingPower);
                        bakerCycle.ExpectedAttestations = expectedAttestations;
                        bakerCycle.FutureAttestationRewards = expectedAttestations * attestationRewardPerSlot;
                    }

                    foreach (var br in brs.Where(x => x.Round == 0))
                    {
                        if (!bakerCycles.TryGetValue(br.Baker, out var bakerCycle))
                            throw new Exception("Nonexistent baker cycle");

                        bakerCycle.FutureBlocks++;
                        bakerCycle.FutureBlockRewards += maxBlockReward;
                    }

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

                    shifted = [..ars.Where(x => x.Level == cycle.LastLevel)];
                }
            }
        }
    }
}
