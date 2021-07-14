using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto1
{
    class StatisticsCommit : ProtocolCommit
    {
        public StatisticsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, IEnumerable<JsonElement> freezerUpdates)
        {
            var prev = await Cache.Statistics.GetAsync(block.Level - 1);
            var statistics = new Statistics
            {
                Level = block.Level,
                TotalActivated = prev.TotalActivated,
                TotalBootstrapped = prev.TotalBootstrapped,
                TotalBurned = prev.TotalBurned,
                TotalBanished = prev.TotalBanished,
                TotalCommitments = prev.TotalCommitments,
                TotalCreated = prev.TotalCreated,
                TotalVested = prev.TotalVested,
                TotalFrozen = prev.TotalFrozen
            };

            if (block.Activations != null)
                statistics.TotalActivated += block.Activations.Sum(x => x.Balance);

            if (block.DoubleBakings != null)
            {
                var lost = block.DoubleBakings.Sum(x => x.OffenderLostDeposit + x.OffenderLostReward + x.OffenderLostFee - x.AccuserReward);
                statistics.TotalBurned += lost;
                statistics.TotalFrozen -= lost;
            }

            if (block.DoubleEndorsings != null)
            {
                var lost = block.DoubleEndorsings.Sum(x => x.OffenderLostDeposit + x.OffenderLostReward + x.OffenderLostFee - x.AccuserReward);
                statistics.TotalBurned += lost;
                statistics.TotalFrozen -= lost;
            }

            if (block.Originations != null)
            {
                var originations = block.Originations.Where(x => x.Status == OperationStatus.Applied);
                if (originations.Any())
                    statistics.TotalBurned += originations.Sum(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            }

            if (block.Transactions != null)
            {
                var transactions = block.Transactions.Where(x => x.Status == OperationStatus.Applied);
                
                if (transactions.Any())
                    statistics.TotalBurned += transactions.Sum(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));

                foreach (var tx in transactions.Where(x => x.Target?.Id == NullAddress.Id))
                    statistics.TotalBanished += tx.Amount;
            }

            if (block.RevelationPenalties != null)
            {
                var lost = block.RevelationPenalties.Sum(x => x.LostReward + x.LostFees);
                statistics.TotalBurned += lost;
                statistics.TotalFrozen -= lost;
            }

            if (block.Endorsements != null)
            {
                var rewards = block.Endorsements.Sum(x => x.Reward);
                var deposits = block.Endorsements.Sum(x => x.Deposit);
                statistics.TotalCreated += rewards;
                statistics.TotalFrozen += deposits;
                statistics.TotalFrozen += rewards;
            }

            if (block.Revelations != null)
            {
                var rewards = block.Revelations.Count * block.Protocol.RevelationReward;
                statistics.TotalCreated += rewards;
                statistics.TotalFrozen += rewards;
            }

            if (block.Migrations != null && block.Migrations.Any(x => x.Kind == MigrationKind.Subsidy))
            {
                var subsidy = block.Migrations.Where(x => x.Kind == MigrationKind.Subsidy).Sum(x => x.BalanceChange);
                statistics.TotalCreated += subsidy;
            }

            statistics.TotalCreated += block.Reward;
            statistics.TotalFrozen += block.Deposit;
            statistics.TotalFrozen += block.Reward;
            statistics.TotalFrozen += block.Fees;

            if (freezerUpdates != null && freezerUpdates.Any())
                statistics.TotalFrozen += freezerUpdates.Sum(x => x.RequiredInt64("change"));

            if (block.Transactions != null)
            {
                var vestedSent = block.Transactions.Where(x => x.Status == OperationStatus.Applied && x.Sender.Type == AccountType.Contract && x.Sender.FirstLevel == 1);
                var vestedReceived = block.Transactions.Where(x => x.Status == OperationStatus.Applied && x.Target.Type == AccountType.Contract && x.Target.FirstLevel == 1);

                if (vestedSent.Any())
                    statistics.TotalVested -= vestedSent.Sum(x => x.Amount);

                if (vestedReceived.Any())
                    statistics.TotalVested += vestedReceived.Sum(x => x.Amount);
            }

            if (block.Events.HasFlag(BlockEvents.CycleEnd))
                statistics.Cycle = block.Cycle;

            if (block.Timestamp.AddSeconds(block.Protocol.TimeBetweenBlocks).Ticks / (10_000_000L * 3600 * 24) != block.Timestamp.Ticks / (10_000_000L * 3600 * 24))
                statistics.Date = block.Timestamp.Date;
            else
            {
                var prevStats = await Cache.Statistics.GetAsync(block.Level - 1);
                if (prevStats.Date == null)
                {
                    var prevBlock = await Cache.Blocks.CurrentAsync();
                    if (prevBlock.Timestamp.Ticks / (10_000_000L * 3600 * 24) != block.Timestamp.Ticks / (10_000_000L * 3600 * 24))
                    {
                        Db.TryAttach(prevStats);
                        prevStats.Date = prevBlock.Timestamp.Date;
                    }
                }
            }

            Db.Statistics.Add(statistics);
            Cache.Statistics.Add(statistics);
        }

        public virtual async Task Revert(Block block)
        {
            await Db.Database.ExecuteSqlRawAsync($@"DELETE FROM ""Statistics"" WHERE ""Level"" = {block.Level}");
            Cache.Statistics.Reset();
        }
    }
}
