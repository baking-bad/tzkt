﻿using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        public async Task<(List<IEnumerable<RightsGenerator.BR>>, List<IEnumerable<RightsGenerator.ER>>)> BootstrapBakingRights(
            Protocol protocol,
            List<Account> accounts,
            List<Cycle> cycles)
        {
            var bakingRights = new List<IEnumerable<RightsGenerator.BR>>(protocol.ConsensusRightsDelay + 1);
            var endorsingRights = new List<IEnumerable<RightsGenerator.ER>>(protocol.ConsensusRightsDelay + 1);

            foreach (var cycle in cycles)
            {
                var (futureBakingRights, futureEndorsingRights) = await GetRights(protocol, accounts, cycle);

                bakingRights.Add(futureBakingRights);
                endorsingRights.Add(futureEndorsingRights);

                var conn = (Db.Database.GetDbConnection() as NpgsqlConnection)!;
                using var writer = conn.BeginBinaryImport(@"
                    COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"")
                    FROM STDIN (FORMAT BINARY)");

                foreach (var er in futureEndorsingRights)
                {
                    writer.StartRow();
                    writer.Write(protocol.GetCycle(er.Level + 1), NpgsqlTypes.NpgsqlDbType.Integer); // level + 1 (shifted)
                    writer.Write(er.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);                    // level + 1 (shifted)
                    writer.Write(er.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((int)BakingRightType.Endorsing, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((int)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.WriteNull();
                    writer.Write(er.Slots, NpgsqlTypes.NpgsqlDbType.Integer);
                }

                foreach (var br in futureBakingRights.SkipWhile(x => x.Level == 1)) // skip bootstrap block rights
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

            return (bakingRights, endorsingRights);
        }

        public async Task ClearBakingRights()
        {
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "BakingRights"
                """);
            Cache.BakingRights.Reset();
        }

        protected virtual async Task<(IEnumerable<RightsGenerator.BR>, IEnumerable<RightsGenerator.ER>)> GetRights(Protocol protocol, List<Account> accounts, Cycle cycle)
        {
            var bakingRights = (await Proto.Rpc.GetBakingRightsAsync(1, cycle.Index))
                .EnumerateArray()
                .Select(x => new RightsGenerator.BR
                {
                    Baker = Cache.Accounts.GetExistingDelegate(x.RequiredString("delegate")).Id,
                    Level = x.RequiredInt32("level"),
                    Round = x.RequiredInt32("priority")
                });

            var endorsingRights = (await Proto.Rpc.GetEndorsingRightsAsync(1, cycle.Index))
                .EnumerateArray()
                .Select(x => new RightsGenerator.ER
                {
                    Baker = Cache.Accounts.GetExistingDelegate(x.RequiredString("delegate")).Id,
                    Level = x.RequiredInt32("level"),
                    Slots = x.RequiredArray("slots").Count()
                });

            return (bakingRights, endorsingRights);
        }
    }
}
