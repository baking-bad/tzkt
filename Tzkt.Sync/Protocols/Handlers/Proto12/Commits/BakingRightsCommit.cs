using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

        public virtual async Task Apply(Block block, Cycle futureCycle)
        {
            #region current rights
            CurrentRights = await Cache.BakingRights.GetAsync(block.Cycle, block.Level);
            var sql = string.Empty;

            if (block.Priority == 0 && block.Validations == block.Protocol.EndorsersPerBlock)
            {
                CurrentRights.RemoveAll(x => x.Type == BakingRightType.Baking && x.Priority > 0);
                CurrentRights.ForEach(x => x.Status = BakingRightStatus.Realized);

                sql = $@"
                    DELETE  FROM ""BakingRights""
                    WHERE   ""Level"" = {block.Level}
                    AND     ""Type"" = {(int)BakingRightType.Baking}
                    AND     ""Priority"" > 0;

                    UPDATE  ""BakingRights""
                    SET     ""Status"" = {(int)BakingRightStatus.Realized}
                    WHERE   ""Level"" = {block.Level};";
            }
            else
            {
                #region load missed priority
                var maxExistedPriority = CurrentRights
                    .Where(x => x.Type == BakingRightType.Baking)
                    .Select(x => x.Priority)
                    .Max();

                if (maxExistedPriority < block.Priority)
                {
                    var cycle = await Db.Cycles.FirstAsync(x => x.Index == block.Cycle);
                    var sampler = await Sampler.CreateAsync(Proto, block.Cycle);
                    var bakingRights = RightsGenerator.GetBakingRights(sampler, cycle, block.Level, block.Priority + 1);

                    var sqlInsert = @"
                        INSERT INTO ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Priority"", ""Slots"") VALUES ";

                    foreach (var br in bakingRights.SkipWhile(x => x.Round <= maxExistedPriority))
                        sqlInsert += $@"
                            ({block.Cycle}, {block.Level}, {br.Baker}, {(int)BakingRightType.Baking}, {(int)BakingRightStatus.Future}, {br.Round}, null),";

                    await Db.Database.ExecuteSqlRawAsync(sqlInsert[..^1]);

                    //TODO: execute sql with RETURNS to get identity
                    var addedRights = await Db.BakingRights
                        .Where(x => x.Level == block.Level && x.Type == BakingRightType.Baking && x.Priority > maxExistedPriority)
                        .ToListAsync();

                    CurrentRights.AddRange(addedRights);
                }
                #endregion

                #region remove excess
                if (CurrentRights.RemoveAll(x => x.Type == BakingRightType.Baking && x.Priority > block.Priority) > 0)
                {
                    sql += $@"
                        DELETE  FROM ""BakingRights""
                        WHERE   ""Level"" = {block.Level}
                        AND     ""Type"" = {(int)BakingRightType.Baking}
                        AND     ""Priority"" > {block.Priority};";
                }
                #endregion

                foreach (var cr in CurrentRights)
                    cr.Status = BakingRightStatus.Missed;

                CurrentRights.First(x => x.Priority == block.Priority).Status = BakingRightStatus.Realized;
                
                if (block.ProposerId != block.BakerId)
                {
                    CurrentRights
                        .Where(x => x.Type == BakingRightType.Baking)
                        .OrderBy(x => x.Priority)
                        .First(x => x.BakerId == block.BakerId)
                        .Status = BakingRightStatus.Realized;
                }

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
                var sampler = await Sampler.CreateAsync(Proto, futureCycle.Index);
                FutureBakingRights = await RightsGenerator.GetBakingRightsAsync(sampler, block.Protocol, futureCycle);
                FutureEndorsingRights = await RightsGenerator.GetEndorsingRightsAsync(sampler, block.Protocol, futureCycle);

                var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
                using var writer = conn.BeginBinaryImport(@"
                    COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Priority"", ""Slots"")
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
    }
}
