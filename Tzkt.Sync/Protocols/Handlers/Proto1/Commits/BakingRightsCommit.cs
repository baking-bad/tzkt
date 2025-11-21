using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class BakingRightsCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public List<BakingRight> CurrentRights { get; protected set; } = null!;
        public IEnumerable<JsonElement>? FutureBakingRights { get; protected set; }
        public IEnumerable<JsonElement>? FutureAttestationRights { get; protected set; }

        public virtual async Task Apply(Block block)
        {
            #region current rights
            CurrentRights = await Cache.BakingRights.GetAsync(block.Level);
            var sql = string.Empty;

            if (block.BlockRound == 0 && block.AttestationPower == block.AttestationCommittee)
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
                    var bakingRights = await Proto.Rpc.GetLevelBakingRightsAsync(block.Level, block.Level, block.BlockRound);
                    //bakingRights = bakingRights.OrderBy(x => x.Round);

                    var sqlInsert = @"
                        INSERT INTO ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"") VALUES ";

                    foreach (var bakingRight in bakingRights.EnumerateArray().SkipWhile(x => x.RequiredInt32("priority") <= maxExistedRound))
                    {
                        var delegat = Cache.Accounts.GetDelegateOrDefault(bakingRight.RequiredString("delegate"));
                        if (delegat == null) continue; // WTF: [level:28680] - Baking rights were given to non-baker account

                        sqlInsert += $@"
                            ({block.Cycle}, {block.Level}, {delegat.Id}, {(int)BakingRightType.Baking}, {(int)BakingRightStatus.Future}, {bakingRight.RequiredInt32("priority")}, null),";
                    }

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
                        WHERE   ""Level"" = {block.Level}
                        AND     ""Id"" = ANY(ARRAY[{string.Join(',', weirdRights.Select(x => x.Id))}]);";
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

                foreach (var ar in FutureAttestationRights)
                    if (!await Cache.Accounts.ExistsAsync(ar.RequiredString("delegate")))
                        throw new Exception($"Account {ar.RequiredString("delegate")} doesn't exist");

                foreach (var br in FutureBakingRights)
                    if (!await Cache.Accounts.ExistsAsync(br.RequiredString("delegate")))
                        throw new Exception($"Account {br.RequiredString("delegate")} doesn't exist");

                var conn = (Db.Database.GetDbConnection() as NpgsqlConnection)!;
                using var writer = conn.BeginBinaryImport(@"COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"") FROM STDIN (FORMAT BINARY)");

                foreach (var ar in FutureAttestationRights)
                {
                    // WTF: [level:28680] - Baking rights were given to non-baker account
                    var acc = await Cache.Accounts.GetExistingAsync(ar.RequiredString("delegate"));
                    
                    writer.StartRow();
                    writer.Write(Context.Protocol.GetCycle(ar.RequiredInt32("level") + 1), NpgsqlTypes.NpgsqlDbType.Integer); // level + 1 (shifted)
                    writer.Write(ar.RequiredInt32("level") + 1, NpgsqlTypes.NpgsqlDbType.Integer);                             // level + 1 (shifted)
                    writer.Write(acc.Id, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((int)BakingRightType.Attestation, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((int)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.WriteNull();
                    writer.Write(ar.RequiredArray("slots").Count(), NpgsqlTypes.NpgsqlDbType.Integer);
                }

                foreach (var br in FutureBakingRights)
                {
                    // WTF: [level:28680] - Baking rights were given to non-baker account
                    var acc = await Cache.Accounts.GetExistingAsync(br.RequiredString("delegate"));

                    writer.StartRow();
                    writer.Write(futureCycle, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(br.RequiredInt32("level"), NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(acc.Id, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((int)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((int)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(br.RequiredInt32("priority"), NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.WriteNull();
                }

                writer.Complete();
            }
            #endregion
        }

        public virtual async Task Revert(Block block)
        {
            #region current rights
            CurrentRights = await Cache.BakingRights.GetAsync(block.Level);

            foreach (var cr in CurrentRights)
                cr.Status = BakingRightStatus.Future;

            await Db.Database.ExecuteSqlRawAsync("""
                UPDATE "BakingRights"
                SET "Status" = {0}
                WHERE "Level" = {1}
                """, (int)BakingRightStatus.Future, block.Level);
            #endregion

            #region new cycle
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
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
            #endregion
        }

        protected virtual async Task<IEnumerable<JsonElement>> GetBakingRights(Block block, Protocol protocol, int cycle)
        {
            var rights = (await Proto.Rpc.GetBakingRightsAsync(block.Level, cycle)).RequiredArray().EnumerateArray();
            if (!rights.Any() || rights.Count(x => x.RequiredInt32("priority") == 0) != protocol.BlocksPerCycle)
                throw new ValidationException("Rpc returned less baking rights (with priority 0) than it should be");

            return rights;
        }

        protected virtual async Task<IEnumerable<JsonElement>> GetAttestationRights(Block block, Protocol protocol, int cycle)
        {
            var rights = (await Proto.Rpc.GetAttestationRightsAsync(block.Level, cycle)).RequiredArray().EnumerateArray();
            if (!rights.Any() || rights.Sum(x => x.RequiredArray("slots").Count()) != protocol.BlocksPerCycle * protocol.AttestersPerBlock)
                throw new ValidationException("Rpc returned less attestation rights (slots) than it should be");

            return rights;
        }
    }
}
