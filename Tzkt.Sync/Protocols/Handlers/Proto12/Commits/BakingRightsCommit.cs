using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class BakingRightsCommit : ProtocolCommit
    {
        public List<BakingRight> CurrentRights { get; protected set; }
        public IEnumerable<RightsGenerator.BR> FutureBakingRights { get; protected set; }
        public IEnumerable<RightsGenerator.ER> FutureEndorsingRights { get; protected set; }

        public BakingRightsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, Cycle futureCycle, Dictionary<int, long> selectedStakes)
        {
            #region current rights
            CurrentRights = await Cache.BakingRights.GetAsync(block.Cycle, block.Level);
            var sql = string.Empty;

            if (block.BlockRound == 0 && block.Validations == block.Protocol.EndorsersPerBlock)
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
                        bakerCycles.Values.Where(x => x.ActiveStake > 0).Select(x => (x.BakerId, x.ActiveStake)),
                        block.ProtoCode > 1 && block.Cycle <= block.Protocol.FirstCycle + block.Protocol.PreservedCycles); //TODO: remove this crutch after ithaca is gone
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
                if (block.ProducerId != block.ProposerId)
                    CurrentRights.First(x => x.Round == block.BlockRound).Status = BakingRightStatus.Realized;

                if (block.Endorsements != null)
                {
                    var endorsers = new HashSet<int>(block.Endorsements.Select(x => x.Delegate.Id));
                    foreach (var er in CurrentRights.Where(x => x.Type == BakingRightType.Endorsing && endorsers.Contains(x.BakerId)))
                        er.Status = BakingRightStatus.Realized;
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
            #endregion

            #region new cycle
            if (futureCycle != null)
            {
                var sampler = GetSampler(
                    selectedStakes.Where(x => x.Value > 0).Select(x => (x.Key, x.Value)),
                    block.Level == block.Protocol.FirstCycleLevel);
                #region temporary diagnostics
                await sampler.Validate(Proto, block.Level, futureCycle.Index);
                #endregion
                FutureBakingRights = await RightsGenerator.GetBakingRightsAsync(sampler, block.Protocol, futureCycle);
                FutureEndorsingRights = await RightsGenerator.GetEndorsingRightsAsync(sampler, block.Protocol, futureCycle);

                var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
                using var writer = conn.BeginBinaryImport(@"
                    COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"")
                    FROM STDIN (FORMAT BINARY)");

                foreach (var er in FutureEndorsingRights)
                {
                    writer.StartRow();
                    writer.Write(block.Protocol.GetCycle(er.Level + 1), NpgsqlTypes.NpgsqlDbType.Integer); // level + 1 (shifted)
                    writer.Write(er.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);                          // level + 1 (shifted)
                    writer.Write(er.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((byte)BakingRightType.Endorsing, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.WriteNull();
                    writer.Write(er.Slots, NpgsqlTypes.NpgsqlDbType.Integer);
                }

                foreach (var br in FutureBakingRights)
                {
                    writer.StartRow();
                    writer.Write(futureCycle.Index, NpgsqlTypes.NpgsqlDbType.Integer);
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
        }

        public virtual async Task Revert(Block block)
        {
            block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);

            #region current rights
            CurrentRights = await Cache.BakingRights.GetAsync(block.Cycle, block.Level);

            foreach (var cr in CurrentRights)
                cr.Status = BakingRightStatus.Future;

            await Db.Database.ExecuteSqlRawAsync($@"
                UPDATE  ""BakingRights""
                SET     ""Status"" = {(int)BakingRightStatus.Future}
                WHERE   ""Level"" = {block.Level}");
            #endregion

            #region new cycle
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                await Db.Database.ExecuteSqlRawAsync($@"
                    DELETE FROM ""BakingRights""
                    WHERE   ""Cycle"" = {block.Cycle + block.Protocol.PreservedCycles} AND ""Type"" = 0
                    OR      ""Level"" > {block.Protocol.GetCycleStart(block.Cycle + block.Protocol.PreservedCycles)}");
            }
            #endregion
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
