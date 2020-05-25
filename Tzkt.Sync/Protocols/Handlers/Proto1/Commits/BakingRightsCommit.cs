using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class BakingRightsCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public List<BakingRight> CurrentRights { get; private set; }
        public List<RawBakingRight> FutureBakingRights { get; private set; }
        public List<RawEndorsingRight> FutureEndorsingRights { get; private set; }

        BakingRightsCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply()
        {
            var cycle = (Block.Level - 1) / Block.Protocol.BlocksPerCycle;

            #region current rights
            CurrentRights = await Cache.BakingRights.GetAsync(cycle, Block.Level);
            var sql = string.Empty;

            // TODO: better use protocol of the block where the endorsing rights were generated
            if (Block.Priority == 0 && Block.Validations == Block.Protocol.EndorsersPerBlock)
            {
                CurrentRights.RemoveAll(x => x.Type == BakingRightType.Baking && x.Priority > 0);
                CurrentRights.ForEach(x => x.Status = BakingRightStatus.Realized);

                sql = $@"
                    DELETE  FROM ""BakingRights""
                    WHERE   ""Level"" = {Block.Level}
                    AND     ""Type"" = {(int)BakingRightType.Baking}
                    AND     ""Priority"" > 0;

                    UPDATE  ""BakingRights""
                    SET     ""Status"" = {(int)BakingRightStatus.Realized}
                    WHERE   ""Level"" = {Block.Level};";
            }
            else
            {
                #region load missed priority
                var maxExistedPriority = CurrentRights
                    .Where(x => x.Type == BakingRightType.Baking)
                    .Select(x => x.Priority)
                    .Max();

                if (maxExistedPriority < Block.Priority)
                {
                    using var stream = await Proto.Node.GetLevelBakingRightsAsync(Block.Level, Block.Priority + 1);
                    var bakingRights = await (Proto.Serializer as Serializer).DeserializeBakingRights(stream);
                    //bakingRights = bakingRights.OrderBy(x => x.Priority);

                    var sqlInsert = @"
                        INSERT INTO ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Priority"", ""Slots"") VALUES ";

                    foreach (var bakingRight in bakingRights.SkipWhile(x => x.Priority <= maxExistedPriority))
                    {
                        var delegat = Cache.Accounts.GetDelegateOrDefault(bakingRight.Delegate);
                        if (delegat == null) continue; // WTF: [level:28680] - Baking rights were given to non-baker account

                        sqlInsert += $@"
                            ({cycle}, {Block.Level}, {delegat.Id}, {(int)BakingRightType.Baking}, {(int)BakingRightStatus.Future}, {bakingRight.Priority}, null),";
                    }

                    await Db.Database.ExecuteSqlRawAsync(sqlInsert[..^1]);

                    //TODO: execute sql with RETURNS to get identity
                    var addedRights = await Db.BakingRights
                        .Where(x => x.Level == Block.Level && x.Type == BakingRightType.Baking && x.Priority > maxExistedPriority)
                        .ToListAsync();

                    CurrentRights.AddRange(addedRights);
                }
                #endregion

                #region remove excess
                if (CurrentRights.RemoveAll(x => x.Type == BakingRightType.Baking && x.Priority > Block.Priority) > 0)
                {
                    sql += $@"
                        DELETE  FROM ""BakingRights""
                        WHERE   ""Level"" = {Block.Level}
                        AND     ""Type"" = {(int)BakingRightType.Baking}
                        AND     ""Priority"" > {Block.Priority};";
                }
                #endregion

                #region remove weird
                var weirdRights = CurrentRights
                    .Where(x => !Cache.Accounts.DelegateExists(x.BakerId))
                    .ToList();

                if (weirdRights.Count > 0)
                {
                    foreach (var wr in weirdRights)
                        CurrentRights.Remove(wr);

                    sql += $@"
                        DELETE  FROM ""BakingRights""
                        WHERE   ""Level"" = {Block.Level}
                        AND     ""Id"" = ANY(ARRAY[{string.Join(',', weirdRights.Select(x => x.Id))}]);";
                }
                #endregion

                foreach (var cr in CurrentRights)
                    cr.Status = BakingRightStatus.Missed;

                CurrentRights.First(x => x.Priority == Block.Priority).Status = BakingRightStatus.Realized;
                
                if (Block.Endorsements != null)
                {
                    var endorsers = new HashSet<int>(Block.Endorsements.Select(x => x.Delegate.Id));
                    foreach (var er in CurrentRights.Where(x => x.Type == BakingRightType.Endorsing && endorsers.Contains(x.BakerId)))
                        er.Status = BakingRightStatus.Realized;
                }

                foreach (var cr in CurrentRights.Where(x => x.Status == BakingRightStatus.Missed))
                {
                    var baker = Cache.Accounts.GetDelegate(cr.BakerId);
                    var available = baker.Balance - baker.FrozenDeposits - baker.FrozenRewards - baker.FrozenFees;
                    var required = cr.Type == BakingRightType.Baking ? Block.Protocol.BlockDeposit : Block.Protocol.EndorsementDeposit;

                    if (available < required)
                        cr.Status = BakingRightStatus.Uncovered;
                }

                var realized = CurrentRights.Where(x => x.Status == BakingRightStatus.Realized);
                var uncovered = CurrentRights.Where(x => x.Status == BakingRightStatus.Uncovered);
                var missed = CurrentRights.Where(x => x.Status == BakingRightStatus.Missed);

                sql += $@"
                    UPDATE  ""BakingRights""
                    SET     ""Status"" = {(int)BakingRightStatus.Realized}
                    WHERE   ""Level"" = {Block.Level}
                    AND     ""Id"" = ANY(ARRAY[{string.Join(',', realized.Select(x => x.Id))}]);";

                if (uncovered.Any())
                {
                    sql += $@"
                        UPDATE  ""BakingRights""
                        SET     ""Status"" = {(int)BakingRightStatus.Uncovered}
                        WHERE   ""Level"" = {Block.Level}
                        AND     ""Id"" = ANY(ARRAY[{string.Join(',', uncovered.Select(x => x.Id))}]);";
                }

                if (missed.Any())
                {
                    sql += $@"
                        UPDATE  ""BakingRights""
                        SET     ""Status"" = {(int)BakingRightStatus.Missed}
                        WHERE   ""Level"" = {Block.Level}
                        AND     ""Id"" = ANY(ARRAY[{string.Join(',', missed.Select(x => x.Id))}]);";
                }
            }

            await Db.Database.ExecuteSqlRawAsync(sql);
            #endregion

            #region new cycle
            if (Block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                var futureCycle = cycle + Block.Protocol.PreservedCycles;

                using var bakingRightsStream = await Proto.Node.GetBakingRightsAsync(Block.Level, futureCycle, BakingRight.MaxPriority + 1);
                FutureBakingRights = await (Proto.Serializer as Serializer).DeserializeBakingRights(bakingRightsStream);

                using var endorsingRightsStream = await Proto.Node.GetEndorsingRightsAsync(Block.Level, futureCycle);
                FutureEndorsingRights = await (Proto.Serializer as Serializer).DeserializeEndorsingRights(endorsingRightsStream);

                foreach (var er in FutureEndorsingRights)
                    if (!await Cache.Accounts.ExistsAsync(er.Delegate))
                        throw new Exception($"Account {er.Delegate} doesn't exist");

                foreach (var br in FutureBakingRights)
                    if (!await Cache.Accounts.ExistsAsync(br.Delegate))
                        throw new Exception($"Account {br.Delegate} doesn't exist");

                var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
                using var writer = conn.BeginBinaryImport(@"COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Priority"", ""Slots"") FROM STDIN (FORMAT BINARY)");

                foreach (var er in FutureEndorsingRights)
                {
                    // WTF: [level:28680] - Baking rights were given to non-baker account
                    var acc = await Cache.Accounts.GetAsync(er.Delegate);
                    
                    writer.StartRow();
                    writer.Write(er.Level / Block.Protocol.BlocksPerCycle, NpgsqlTypes.NpgsqlDbType.Integer); // level + 1 (shifted)
                    writer.Write(er.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);                             // level + 1 (shifted)
                    writer.Write(acc.Id, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((byte)BakingRightType.Endorsing, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.WriteNull();
                    writer.Write(er.Slots.Count, NpgsqlTypes.NpgsqlDbType.Integer);
                }

                foreach (var br in FutureBakingRights)
                {
                    // WTF: [level:28680] - Baking rights were given to non-baker account
                    var acc = await Cache.Accounts.GetAsync(br.Delegate);

                    writer.StartRow();
                    writer.Write(futureCycle, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(br.Level, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(acc.Id, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((byte)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write(br.Priority, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.WriteNull();
                }

                writer.Complete();
            }
            #endregion
        }

        public override async Task Revert()
        {
            Block.Protocol ??= await Cache.Protocols.GetAsync(Block.ProtoCode);
            var cycle = (Block.Level - 1) / Block.Protocol.BlocksPerCycle;

            #region current rights
            CurrentRights = await Cache.BakingRights.GetAsync(cycle, Block.Level);

            foreach (var cr in CurrentRights)
                cr.Status = BakingRightStatus.Future;

            await Db.Database.ExecuteSqlRawAsync($@"
                UPDATE  ""BakingRights""
                SET     ""Status"" = {(int)BakingRightStatus.Future}
                WHERE   ""Level"" = {Block.Level}");
            #endregion

            #region new cycle
            if (Block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                await Db.Database.ExecuteSqlRawAsync($@"
                    DELETE FROM ""BakingRights""
                    WHERE   ""Cycle"" = {cycle + Block.Protocol.PreservedCycles} AND ""Type"" = 0
                    OR      ""Level"" > {(cycle + Block.Protocol.PreservedCycles) * Block.Protocol.BlocksPerCycle + 1}");
            }
            #endregion
        }

        #region static
        public static async Task<BakingRightsCommit> Apply(ProtocolHandler proto, Block block)
        {
            var commit = new BakingRightsCommit(proto) { Block = block };
            await commit.Apply();
            return commit;
        }

        public static async Task<BakingRightsCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new BakingRightsCommit(proto) { Block = block };
            await commit.Revert();
            return commit;
        }
        #endregion
    }
}
