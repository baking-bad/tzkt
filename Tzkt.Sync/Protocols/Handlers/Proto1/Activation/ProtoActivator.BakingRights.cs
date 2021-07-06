using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        public async Task<(List<JsonElement>, List<JsonElement>)> BootstrapBakingRights(Protocol protocol, List<Account> accounts)
        {
            var bakingRights = new List<JsonElement>(protocol.PreservedCycles + 1);
            var endorsingRights = new List<JsonElement>(protocol.PreservedCycles + 1);

            var delegates = accounts
                .Where(x => x.Type == AccountType.Delegate)
                .ToDictionary(k => k.Address, v => v.Id);

            for (int cycle = 0; cycle <= protocol.PreservedCycles; cycle++)
            {
                var rawBakingRights = await Proto.Rpc.GetBakingRightsAsync(1, cycle);
                var rawEndorsingRights = await Proto.Rpc.GetEndorsingRightsAsync(1, cycle);

                bakingRights.Add(rawBakingRights);
                endorsingRights.Add(rawEndorsingRights);

                var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
                using var writer = conn.BeginBinaryImport(
                    @"COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Priority"", ""Slots"") FROM STDIN (FORMAT BINARY)");

                foreach (var er in rawEndorsingRights.EnumerateArray())
                {
                    writer.StartRow();
                    writer.Write(protocol.GetCycle(er.RequiredInt32("level") + 1), NpgsqlTypes.NpgsqlDbType.Integer); // level + 1 (shifted)
                    writer.Write(er.RequiredInt32("level") + 1, NpgsqlTypes.NpgsqlDbType.Integer);                    // level + 1 (shifted)
                    writer.Write(delegates[er.RequiredString("delegate")], NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((byte)BakingRightType.Endorsing, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.WriteNull();
                    writer.Write(er.RequiredArray("slots").Count(), NpgsqlTypes.NpgsqlDbType.Integer);
                }

                var skipLevel = endorsingRights[cycle][0].RequiredInt32("level"); // skip bootstrap block rights
                foreach (var br in rawBakingRights.EnumerateArray().SkipWhile(x => cycle == 0 && x.RequiredInt32("level") == skipLevel))
                {
                    writer.StartRow();
                    writer.Write(cycle, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(br.RequiredInt32("level"), NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(delegates[br.RequiredString("delegate")], NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((byte)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write(br.RequiredInt32("priority"), NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.WriteNull();
                }

                writer.Complete();
            }

            return (bakingRights, endorsingRights);
        }

        public async Task ClearBakingRights()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BakingRights""");
        }
    }
}
