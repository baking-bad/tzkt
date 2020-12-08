using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        async Task BootstrapCommitments(JToken parameters)
        {
            var commitments = parameters["commitments"]?.Select(x => new Commitment
            {
                Address = x[0].Value<string>(),
                Balance = x[1].Value<long>()
            });

            if (commitments != null)
            {
                var state = Cache.AppState.Get();
                var statistics = await Cache.Statistics.GetAsync(1);

                var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
                using var writer = conn.BeginBinaryImport(@"COPY ""Commitments"" (""Balance"", ""Address"") FROM STDIN (FORMAT BINARY)");

                foreach (var commitment in commitments)
                {
                    writer.StartRow();
                    writer.Write(commitment.Balance);
                    writer.Write(commitment.Address);

                    state.CommitmentsCount++;
                    statistics.TotalCommitments += commitment.Balance;
                }

                writer.Complete();
            }
        }

        async Task ClearCommitments()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""Commitments""");

            var state = Cache.AppState.Get();
            state.CommitmentsCount = 0;
        }
    }
}
