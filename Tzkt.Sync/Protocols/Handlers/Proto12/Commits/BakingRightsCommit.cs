using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class BakingRightsCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public List<BakingRight> CurrentRights { get; protected set; } = null!;
        public IEnumerable<RightsGenerator.BR>? FutureBakingRights { get; protected set; }
        public IEnumerable<RightsGenerator.AR>? FutureAttestationRights { get; protected set; }

        public virtual async Task Apply(Block block, Cycle? futureCycle, Dictionary<int, long>? selectedStakes)
        {
            await ApplyCurrentRights(block);

            if (futureCycle != null)
                await ApplyNewCycle(block, futureCycle, selectedStakes!);
        }

        protected virtual async Task ApplyCurrentRights(Block block)
        {
            CurrentRights = await Cache.BakingRights.GetAsync(block.Level);
            var sql = string.Empty;

            if (block.BlockRound == 0 && block.Validations == Context.Protocol.AttestersPerBlock)
            {
                CurrentRights.RemoveAll(x => x.Type == BakingRightType.Baking && x.Round > 0);
                CurrentRights.ForEach(x => x.Status = BakingRightStatus.Realized);

                sql = $@"
                    DELETE  FROM ""BakingRights""
                    WHERE   ""Level"" = {block.Level}
                    AND     ""Type"" = {(int)BakingRightType.Baking}
                    AND     ""Round"" > 0;

                    UPDATE  ""BakingRights""
                    SET     ""Status"" = {(int)BakingRightStatus.Realized}
                    WHERE   ""Level"" = {block.Level};";
            }
            else
            {
                #region load missed rounds
                var maxExistedRound = CurrentRights
                    .Where(x => x.Type == BakingRightType.Baking)
                    .Select(x => x.Round)
                    .Max();

                if (maxExistedRound < block.BlockRound)
                {
                    var cycle = await Db.Cycles.FirstAsync(x => x.Index == block.Cycle);
                    var bakerCycles = await Cache.BakerCycles.GetAsync(block.Cycle);
                    var sampler = GetSampler(
                        bakerCycles.Values.Where(x => x.BakingPower > 0).Select(x => (x.BakerId, x.BakingPower)),
                        block.ProtoCode > 1 && block.Cycle <= Context.Protocol.FirstCycle + Context.Protocol.ConsensusRightsDelay); //TODO: remove this crutch after ithaca is gone
                    #region temporary diagnostics
                    await sampler.Validate(Proto, block.Level, block.Cycle);
                    #endregion
                    var bakingRights = RightsGenerator.GetBakingRights(sampler, cycle, block.Level, block.BlockRound + 1);

                    var sqlInsert = @"
                        INSERT INTO ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"") VALUES ";

                    foreach (var br in bakingRights.SkipWhile(x => x.Round <= maxExistedRound))
                        sqlInsert += $@"
                            ({block.Cycle}, {block.Level}, {br.Baker}, {(int)BakingRightType.Baking}, {(int)BakingRightStatus.Future}, {br.Round}, null),";

                    await Db.Database.ExecuteSqlRawAsync(sqlInsert[..^1]);

                    //TODO: execute sql with RETURNS to get identity
                    var addedRights = await Db.BakingRights
                        .Where(x => x.Level == block.Level && x.Type == BakingRightType.Baking && x.Round > maxExistedRound)
                        .ToListAsync();

                    CurrentRights.AddRange(addedRights);
                }
                #endregion

                #region remove excess
                if (CurrentRights.RemoveAll(x => x.Type == BakingRightType.Baking && x.Round > block.BlockRound) > 0)
                {
                    sql += $@"
                        DELETE  FROM ""BakingRights""
                        WHERE   ""Level"" = {block.Level}
                        AND     ""Type"" = {(int)BakingRightType.Baking}
                        AND     ""Round"" > {block.BlockRound};";
                }
                #endregion

                foreach (var cr in CurrentRights)
                    cr.Status = BakingRightStatus.Missed;

                CurrentRights.First(x => x.Round == block.PayloadRound).Status = BakingRightStatus.Realized;
                if (block.PayloadRound != block.BlockRound)
                    CurrentRights.First(x => x.Round == block.BlockRound).Status = BakingRightStatus.Realized;

                if (Context.AttestationOps.Count != 0)
                {
                    var attesters = new HashSet<int>(Context.AttestationOps.Select(x => x.DelegateId));
                    foreach (var ar in CurrentRights.Where(x => x.Type == BakingRightType.Attestation && attesters.Contains(x.BakerId)))
                        ar.Status = BakingRightStatus.Realized;
                }

                var realized = CurrentRights.Where(x => x.Status == BakingRightStatus.Realized);
                var missed = CurrentRights.Where(x => x.Status == BakingRightStatus.Missed);

                sql += $@"
                    UPDATE  ""BakingRights""
                    SET     ""Status"" = {(int)BakingRightStatus.Realized}
                    WHERE   ""Level"" = {block.Level}
                    AND     ""Id"" = ANY(ARRAY[{string.Join(',', realized.Select(x => x.Id))}]);";

                if (missed.Any())
                {
                    sql += $@"
                        UPDATE  ""BakingRights""
                        SET     ""Status"" = {(int)BakingRightStatus.Missed}
                        WHERE   ""Level"" = {block.Level}
                        AND     ""Id"" = ANY(ARRAY[{string.Join(',', missed.Select(x => x.Id))}]);";
                }
            }

            await Db.Database.ExecuteSqlRawAsync(sql);
        }

        protected virtual async Task ApplyNewCycle(Block block, Cycle futureCycle, Dictionary<int, long> selectedStakes)
        {
            var sampler = GetSampler(
                selectedStakes.Where(x => x.Value > 0).Select(x => (x.Key, x.Value)),
                block.Level == Context.Protocol.FirstCycleLevel);

            #region temporary diagnostics
            await sampler.Validate(Proto, block.Level, futureCycle.Index);
            #endregion

            FutureBakingRights = await RightsGenerator.GetBakingRightsAsync(sampler, Context.Protocol, futureCycle);
            FutureAttestationRights = await RightsGenerator.GetAttestationRightsAsync(sampler, Context.Protocol, futureCycle);

            var conn = (Db.Database.GetDbConnection() as NpgsqlConnection)!;
            using var writer = conn.BeginBinaryImport(@"
                COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"")
                FROM STDIN (FORMAT BINARY)");

            foreach (var ar in FutureAttestationRights)
            {
                writer.StartRow();
                writer.Write(Context.Protocol.GetCycle(ar.Level + 1), NpgsqlTypes.NpgsqlDbType.Integer); // level + 1 (shifted)
                writer.Write(ar.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);                          // level + 1 (shifted)
                writer.Write(ar.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((int)BakingRightType.Attestation, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((int)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.WriteNull();
                writer.Write(ar.Slots, NpgsqlTypes.NpgsqlDbType.Integer);
            }

            foreach (var br in FutureBakingRights)
            {
                writer.StartRow();
                writer.Write(futureCycle.Index, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(br.Level, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(br.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((int)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((int)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(br.Round, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.WriteNull();
            }

            writer.Complete();
        }

        public virtual async Task Revert(Block block)
        {
            await RevertCurrentRights(block);

            if (block.Events.HasFlag(BlockEvents.CycleBegin))
                await RevertNewCycle(block);
        }

        public virtual async Task RevertCurrentRights(Block block)
        {
            CurrentRights = await Cache.BakingRights.GetAsync(block.Level);

            foreach (var cr in CurrentRights)
                cr.Status = BakingRightStatus.Future;

            await Db.Database.ExecuteSqlRawAsync("""
                UPDATE "BakingRights"
                SET "Status" = {0}
                WHERE "Level" = {1}
                """, (int)BakingRightStatus.Future, block.Level);
        }

        public virtual async Task RevertNewCycle(Block block)
        {
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "BakingRights"
                WHERE "Cycle" = {0} AND "Type" = {1}
                OR "Level" > {2}
                """,
                block.Cycle + Context.Protocol.ConsensusRightsDelay,
                (int)BakingRightType.Baking,
                Context.Protocol.GetCycleStart(block.Cycle + Context.Protocol.ConsensusRightsDelay));
        }

        protected virtual Sampler GetSampler(IEnumerable<(int id, long stake)> selection, bool forceBase)
        {
            var sorted = selection
                .OrderByDescending(x => x.stake)
                .ThenByDescending(x => Base58.Parse(Cache.Accounts.GetDelegate(x.id).Address), new BytesComparer());

            return new Sampler(sorted.Select(x => x.id).ToArray(), sorted.Select(x => x.stake).ToArray());
        }
    }
}
