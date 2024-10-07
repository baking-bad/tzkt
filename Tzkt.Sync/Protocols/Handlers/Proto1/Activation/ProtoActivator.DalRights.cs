using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        public async Task BootstrapDalRights(
            Protocol protocol,
            List<Account> accounts,
            List<Cycle> cycles)
        {
            foreach (var cycle in cycles)
            {
                var futureDalRights = await GetDalRights(protocol, accounts, cycle);

                var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
                using var writer = conn.BeginBinaryImport(@"
                    COPY ""DalRights"" (""Cycle"", ""Level"", ""DelegateId"", ""Shards"")
                    FROM STDIN (FORMAT BINARY)");

                foreach (var dr in futureDalRights)
                {
                    writer.StartRow();
                    writer.Write(protocol.GetCycle(dr.Level + 1), NpgsqlTypes.NpgsqlDbType.Integer); // level + 1 (shifted)
                    writer.Write(dr.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);                    // level + 1 (shifted)
                    writer.Write(dr.Delegate, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(dr.Shards, NpgsqlTypes.NpgsqlDbType.Integer);
                }

                writer.Complete();
            }
        }

        public async Task ClearDalRights()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""DalRights""");
        }

        protected virtual async Task<IEnumerable<RightsGenerator.DR>> GetDalRights(Protocol protocol, List<Account> accounts, Cycle cycle)
        {
            return await Task.FromResult(Enumerable.Empty<RightsGenerator.DR>());
        }
    }
}
