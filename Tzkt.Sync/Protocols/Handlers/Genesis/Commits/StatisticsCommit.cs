using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Genesis
{
    class StatisticsCommit : ProtocolCommit
    {
        StatisticsCommit(ProtocolHandler protocol) : base(protocol) { }

        public override Task Apply()
        {
            var stats = new Statistics();
            
            Db.Statistics.Add(stats);
            Cache.Statistics.Add(stats);

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            return Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""Statistics""");
        }

        #region static
        public static async Task<StatisticsCommit> Apply(ProtocolHandler proto)
        {
            var commit = new StatisticsCommit(proto);
            await commit.Apply();

            return commit;
        }

        public static async Task<StatisticsCommit> Revert(ProtocolHandler proto)
        {
            var commit = new StatisticsCommit(proto);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
