using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Initiator
{
    class StatisticsCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public Statistics Statistics { get; private set; }

        StatisticsCommit(ProtocolHandler protocol) : base(protocol) { }

        public Task Init(Block block, List<Account> accounts, IEnumerable<Commitment> commitments)
        {
            Block = block;
            Statistics = new Statistics
            {
                Level = block.Level,
                TotalBootstrapped = accounts.Sum(x => x.Balance),
                TotalCommitments = commitments?.Sum(x => x.Balance) ?? 0,
                TotalVested = accounts.Where(x => x.Type == AccountType.Contract).Sum(x => x.Balance)
            };

            return Task.CompletedTask;
        }

        public override async Task Apply()
        {
            if (Block.Level % Block.Protocol.BlocksPerCycle == 0)
                Statistics.Cycle = (Block.Level - 1) / Block.Protocol.BlocksPerCycle;

            if (Block.Timestamp.AddSeconds(Block.Protocol.TimeBetweenBlocks).Ticks / (10_000_000L * 3600 * 24) != Block.Timestamp.Ticks / (10_000_000L * 3600 * 24))
                Statistics.Date = Block.Timestamp.Date;
            else
            {
                var prevStats = await Cache.Statistics.GetAsync(Block.Level - 1);
                if (prevStats.Date == null)
                {
                    var prevBlock = await Cache.Blocks.CurrentAsync();
                    if (prevBlock.Timestamp.Ticks / (10_000_000L * 3600 * 24) != Block.Timestamp.Ticks / (10_000_000L * 3600 * 24))
                    {
                        Db.TryAttach(prevStats);
                        prevStats.Date = prevBlock.Timestamp.Date;
                    }
                }
            }

            Db.Statistics.Add(Statistics);
            Cache.Statistics.Add(Statistics);
        }

        public override async Task Revert()
        {
            var state = Cache.AppState.Get();
            await Db.Database.ExecuteSqlRawAsync($@"DELETE FROM ""Statistics"" WHERE ""Level"" = {state.Level}");
            Cache.Statistics.Reset();
        }

        #region static
        public static async Task<StatisticsCommit> Apply(ProtocolHandler proto, Block block, List<Account> accounts, IEnumerable<Commitment> commitments)
        {
            var commit = new StatisticsCommit(proto);
            await commit.Init(block, accounts, commitments);
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
