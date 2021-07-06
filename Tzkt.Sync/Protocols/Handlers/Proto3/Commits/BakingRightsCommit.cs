using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class BakingRightsCommit : Proto1.BakingRightsCommit
    {
        public BakingRightsCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply(Block block)
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
                    var bakingRights = await Proto.Rpc.GetLevelBakingRightsAsync(block.Level, block.Priority);
                    //bakingRights = bakingRights.OrderBy(x => x.Priority);

                    var sqlInsert = @"
                        INSERT INTO ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Priority"", ""Slots"") VALUES ";

                    foreach (var br in bakingRights.EnumerateArray().SkipWhile(x => x.RequiredInt32("priority") <= maxExistedPriority))
                        sqlInsert += $@"
                            ({block.Cycle}, {block.Level}, {Cache.Accounts.GetDelegate(br.RequiredString("delegate")).Id}, {(int)BakingRightType.Baking}, {(int)BakingRightStatus.Future}, {br.RequiredInt32("priority")}, null),";

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

                if (block.Endorsements != null)
                {
                    var endorsers = new HashSet<int>(block.Endorsements.Select(x => x.Delegate.Id));
                    foreach (var er in CurrentRights.Where(x => x.Type == BakingRightType.Endorsing && endorsers.Contains(x.BakerId)))
                        er.Status = BakingRightStatus.Realized;
                }

                foreach (var cr in CurrentRights.Where(x => x.Status == BakingRightStatus.Missed))
                {
                    var baker = Cache.Accounts.GetDelegate(cr.BakerId);
                    var available = baker.Balance - baker.FrozenDeposits - baker.FrozenRewards - baker.FrozenFees;
                    var required = cr.Type == BakingRightType.Baking
                        ? (block.Cycle < block.Protocol.RampUpCycles
                            ? (block.Protocol.BlockDeposit * block.Cycle / block.Protocol.RampUpCycles)
                            : block.Protocol.BlockDeposit)
                        : (block.Cycle < block.Protocol.RampUpCycles
                            ? (cr.Slots * block.Protocol.EndorsementDeposit * block.Cycle / block.Protocol.RampUpCycles)
                            : (cr.Slots * block.Protocol.EndorsementDeposit));

                    if (available < required)
                        cr.Status = BakingRightStatus.Uncovered;
                }

                var realized = CurrentRights.Where(x => x.Status == BakingRightStatus.Realized);
                var uncovered = CurrentRights.Where(x => x.Status == BakingRightStatus.Uncovered);
                var missed = CurrentRights.Where(x => x.Status == BakingRightStatus.Missed);

                sql += $@"
                    UPDATE  ""BakingRights""
                    SET     ""Status"" = {(int)BakingRightStatus.Realized}
                    WHERE   ""Level"" = {block.Level}
                    AND     ""Id"" = ANY(ARRAY[{string.Join(',', realized.Select(x => x.Id))}]);";

                if (uncovered.Any())
                {
                    sql += $@"
                        UPDATE  ""BakingRights""
                        SET     ""Status"" = {(int)BakingRightStatus.Uncovered}
                        WHERE   ""Level"" = {block.Level}
                        AND     ""Id"" = ANY(ARRAY[{string.Join(',', uncovered.Select(x => x.Id))}]);";
                }

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
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                var futureCycle = block.Cycle + block.Protocol.PreservedCycles;

                FutureBakingRights = (await Proto.Rpc.GetBakingRightsAsync(block.Level, futureCycle)).EnumerateArray();
                FutureEndorsingRights = (await Proto.Rpc.GetEndorsingRightsAsync(block.Level, futureCycle)).EnumerateArray();

                var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
                using var writer = conn.BeginBinaryImport(@"COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Priority"", ""Slots"") FROM STDIN (FORMAT BINARY)");

                foreach (var er in FutureEndorsingRights)
                {
                    writer.StartRow();
                    writer.Write(block.Protocol.GetCycle(er.RequiredInt32("level") + 1), NpgsqlTypes.NpgsqlDbType.Integer); // level + 1 (shifted)
                    writer.Write(er.RequiredInt32("level") + 1, NpgsqlTypes.NpgsqlDbType.Integer);                          // level + 1 (shifted)
                    writer.Write(Cache.Accounts.GetDelegate(er.RequiredString("delegate")).Id, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((byte)BakingRightType.Endorsing, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.WriteNull();
                    writer.Write(er.RequiredArray("slots").Count(), NpgsqlTypes.NpgsqlDbType.Integer);
                }

                foreach (var br in FutureBakingRights)
                {
                    writer.StartRow();
                    writer.Write(futureCycle, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(br.RequiredInt32("level"), NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(Cache.Accounts.GetDelegate(br.RequiredString("delegate")).Id, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((byte)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write(br.RequiredInt32("priority"), NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.WriteNull();
                }

                writer.Complete();
            }
            #endregion
        }
    }
}
