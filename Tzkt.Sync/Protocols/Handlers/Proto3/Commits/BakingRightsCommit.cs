using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class BakingRightsCommit(ProtocolHandler protocol) : Proto1.BakingRightsCommit(protocol)
    {
        public override async Task Apply(Block block)
        {
            #region current rights
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
                    var bakingRights = await Proto.Rpc.GetLevelBakingRightsAsync(block.Level - 1, block.Level, block.BlockRound);
                    //bakingRights = bakingRights.OrderBy(x => x.Round);

                    var sqlInsert = @"
                        INSERT INTO ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"") VALUES ";

                    foreach (var br in bakingRights.EnumerateArray().SkipWhile(x => x.RequiredInt32("priority") <= maxExistedRound))
                        sqlInsert += $@"
                            ({block.Cycle}, {block.Level}, {Cache.Accounts.GetExistingDelegate(br.RequiredString("delegate")).Id}, {(int)BakingRightType.Baking}, {(int)BakingRightStatus.Future}, {br.RequiredInt32("priority")}, null),";

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
            #endregion

            #region new cycle
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                var futureCycle = block.Cycle + Context.Protocol.ConsensusRightsDelay;

                FutureBakingRights = await GetBakingRights(block, Context.Protocol, futureCycle);
                FutureAttestationRights = await GetAttestationRights(block, Context.Protocol, futureCycle);

                var conn = (Db.Database.GetDbConnection() as NpgsqlConnection)!;
                using var writer = conn.BeginBinaryImport(@"COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"") FROM STDIN (FORMAT BINARY)");

                foreach (var ar in FutureAttestationRights)
                {
                    writer.StartRow();
                    writer.Write(Context.Protocol.GetCycle(ar.RequiredInt32("level") + 1), NpgsqlTypes.NpgsqlDbType.Integer); // level + 1 (shifted)
                    writer.Write(ar.RequiredInt32("level") + 1, NpgsqlTypes.NpgsqlDbType.Integer);                          // level + 1 (shifted)
                    writer.Write(Cache.Accounts.GetExistingDelegate(ar.RequiredString("delegate")).Id, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((int)BakingRightType.Attestation, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((int)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.WriteNull();
                    writer.Write(ar.RequiredArray("slots").Count(), NpgsqlTypes.NpgsqlDbType.Integer);
                }

                foreach (var br in FutureBakingRights)
                {
                    writer.StartRow();
                    writer.Write(futureCycle, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(br.RequiredInt32("level"), NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(Cache.Accounts.GetExistingDelegate(br.RequiredString("delegate")).Id, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((int)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((int)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(br.RequiredInt32("priority"), NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.WriteNull();
                }

                writer.Complete();
            }
            #endregion
        }
    }
}
