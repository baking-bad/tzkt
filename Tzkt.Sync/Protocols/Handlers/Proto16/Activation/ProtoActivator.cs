using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Netmavryk.Encoding;
using Newtonsoft.Json.Linq;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto16
{
    partial class ProtoActivator : Proto15.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.SmartRollupOriginationSize = parameters["smart_rollup_origination_size"]?.Value<int>() ?? 6_314;
            protocol.SmartRollupStakeAmount = long.Parse(parameters["smart_rollup_stake_amount"]?.Value<string>() ?? "10000000000");
            protocol.SmartRollupChallengeWindow = parameters["smart_rollup_challenge_window_in_blocks"]?.Value<int>() ?? 80_640;
            protocol.SmartRollupCommitmentPeriod = parameters["smart_rollup_commitment_period_in_blocks"]?.Value<int>() ?? 60;
            protocol.SmartRollupTimeoutPeriod = parameters["smart_rollup_timeout_period_in_blocks"]?.Value<int>() ?? 40_320;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            protocol.BlocksPerCycle = prev.BlocksPerCycle * 2;
            protocol.BlocksPerCommitment = prev.BlocksPerCommitment * 2;
            protocol.BlocksPerSnapshot = prev.BlocksPerSnapshot * 2;
            protocol.BlocksPerVoting = prev.BlocksPerVoting * 2;
            protocol.HardBlockGasLimit = prev.HardBlockGasLimit / 2;
            protocol.TimeBetweenBlocks = (prev.TimeBetweenBlocks + 1) / 2;

            var totalReward = 80_000_000 * protocol.TimeBetweenBlocks / 60;
            protocol.BlockReward0 = totalReward / 4;
            protocol.BlockReward1 = totalReward / 4 / (protocol.EndorsersPerBlock / 3);
            protocol.EndorsementReward0 = totalReward / 2 / protocol.EndorsersPerBlock;
            protocol.EndorsementReward1 = 0;

            protocol.SmartRollupOriginationSize = 6_314;
            protocol.SmartRollupStakeAmount = 10_000_000_000;
            protocol.SmartRollupChallengeWindow = 80_640;
            protocol.SmartRollupCommitmentPeriod = 60;
            protocol.SmartRollupTimeoutPeriod = 40_320;

            protocol.MaxBakingReward = protocol.BlockReward0 + protocol.EndorsersPerBlock / 3 * protocol.BlockReward1;
            protocol.MaxEndorsingReward = protocol.EndorsersPerBlock * protocol.EndorsementReward0;
        }

        protected override async Task ActivateContext(AppState state)
        {
            await base.ActivateContext(state);
            new InboxCommit(Proto).Init(Cache.Blocks.Current());
        }

        protected override async Task DeactivateContext(AppState state)
        {
            await base.DeactivateContext(state);
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "InboxMessages"
                """);
            Cache.AppState.Get().InboxMessageCounter = 0;
        }

        protected override async Task MigrateContext(AppState state)
        {
            var prevProto = await Cache.Protocols.GetAsync(state.Protocol);
            var nextProto = await Cache.Protocols.GetAsync(state.NextProtocol);

            MigrateBakers(state, prevProto, nextProto);
            await MigrateVotingPeriods(state, nextProto);
            var cycles = await MigrateCycles(state, nextProto);
            await MigrateFutureRights(state, nextProto, cycles);

            Cache.BakingRights.Reset();
            Cache.BakerCycles.Reset();
            Cache.Periods.Reset();

            new InboxCommit(Proto).Init(Cache.Blocks.Current());
        }

        protected override Task RevertContext(AppState state)
        {
            throw new NotImplementedException("Reverting the migration block is not implemented, because likely won't be needed");
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

            foreach (var cycle in cycles.Where(x => x.Index > state.Cycle))
            {
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
                var sampler = GetOldSampler(bakerCycles.Values
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
                        bakerCycle.FutureBlocks = 0;
                        bakerCycle.FutureBlockRewards = 0;
                        bakerCycle.FutureEndorsements = 0;
                        
                        var expectedEndorsements = (int)(new BigInteger(nextProto.BlocksPerCycle) * nextProto.EndorsersPerBlock * bakerCycle.BakingPower / cycle.TotalBakingPower);
                        bakerCycle.ExpectedBlocks = nextProto.BlocksPerCycle * bakerCycle.BakingPower / cycle.TotalBakingPower;
                        bakerCycle.ExpectedEndorsements = expectedEndorsements;
                        bakerCycle.FutureEndorsementRewards = expectedEndorsements * nextProto.EndorsementReward0;
                    }

                    foreach (var br in brs.Where(x => x.Round == 0))
                    {
                        if (!bakerCycles.TryGetValue(br.Baker, out var bakerCycle))
                            throw new Exception("Nonexistent baker cycle");

                        bakerCycle.FutureBlocks++;
                        bakerCycle.FutureBlockRewards += nextProto.MaxBakingReward;
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

        Sampler GetOldSampler(IEnumerable<(int id, long stake)> selection)
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
