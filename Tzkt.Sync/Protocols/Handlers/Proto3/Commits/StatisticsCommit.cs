using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto3
{
    class StatisticsCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public Statistics Statistics { get; private set; }

        StatisticsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, IEnumerable<IBalanceUpdate> freezerUpdates)
        {
            var prev = await Cache.Statistics.GetAsync(block.Level - 1);

            Block = block;
            Statistics = new Statistics
            {
                Level = block.Level,
                TotalActivated = prev.TotalActivated,
                TotalBootstrapped = prev.TotalBootstrapped,
                TotalBurned = prev.TotalBurned,
                TotalCommitments = prev.TotalCommitments,
                TotalCreated = prev.TotalCreated,
                TotalVested = prev.TotalVested,
                TotalFrozen = prev.TotalFrozen
            };

            if (block.Activations != null)
                Statistics.TotalActivated += block.Activations.Sum(x => x.Balance);

            if (block.DoubleBakings != null)
            {
                var lost = block.DoubleBakings.Sum(x => x.OffenderLostDeposit + x.OffenderLostReward + x.OffenderLostFee - x.AccuserReward);
                Statistics.TotalBurned += lost;
                Statistics.TotalFrozen -= lost;
            }

            if (block.Originations != null)
            {
                var originations = block.Originations.Where(x => x.Status == OperationStatus.Applied);
                if (originations.Any())
                    Statistics.TotalBurned += originations.Sum(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            }

            if (block.Transactions != null)
            {
                var transactions = block.Transactions.Where(x => x.Status == OperationStatus.Applied);
                if (transactions.Any())
                    Statistics.TotalBurned += transactions.Sum(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            }

            if (block.RevelationPenalties != null)
            {
                var lost = block.RevelationPenalties.Sum(x => x.LostReward + x.LostFees);
                Statistics.TotalBurned += lost;
                Statistics.TotalFrozen -= lost;
            }

            if (block.Endorsements != null)
            {
                var rewards = block.Endorsements.Sum(x => x.Reward);
                Statistics.TotalCreated += rewards;
                Statistics.TotalFrozen += rewards;
                Statistics.TotalFrozen += block.Protocol.EndorsementDeposit * block.Endorsements.Sum(x => x.Slots);
            }

            if (block.Revelations != null)
            {
                var rewards = block.Revelations.Count * block.Protocol.RevelationReward;
                Statistics.TotalCreated += rewards;
                Statistics.TotalFrozen += rewards;
            }

            Statistics.TotalCreated += block.Reward;
            Statistics.TotalFrozen += block.Protocol.BlockDeposit;
            Statistics.TotalFrozen += block.Reward;
            Statistics.TotalFrozen += block.Fees;

            if (freezerUpdates != null && freezerUpdates.Any())
                Statistics.TotalFrozen += freezerUpdates.Sum(x => x.Change);

            if (block.Transactions != null)
            {
                var vestedSent = block.Transactions.Where(x => x.Status == OperationStatus.Applied && x.Sender.Type == AccountType.Contract && x.Sender.FirstLevel == 1);
                var vestedReceived = block.Transactions.Where(x => x.Status == OperationStatus.Applied && x.Target.Type == AccountType.Contract && x.Target.FirstLevel == 1);

                if (vestedSent.Any())
                    Statistics.TotalVested -= vestedSent.Sum(x => x.Amount);

                if (vestedReceived.Any())
                    Statistics.TotalVested += vestedReceived.Sum(x => x.Amount);
            }
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
        public static async Task<StatisticsCommit> Apply(ProtocolHandler proto, Block block, IEnumerable<IBalanceUpdate> freezerUpdates)
        {
            var commit = new StatisticsCommit(proto);
            await commit.Init(block, freezerUpdates);
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
