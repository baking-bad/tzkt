using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto12
{
    class StatisticsCommit : ProtocolCommit
    {
        public StatisticsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, List<EndorsingRewardOperation> endorsingRewards, long freezerChange)
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
                TotalFrozen = prev.TotalFrozen,
                TotalRollupBonds = prev.TotalRollupBonds
            };

            statistics.TotalFrozen += freezerChange;

            statistics.TotalCreated += block.Reward;
            statistics.TotalCreated += block.Bonus;

            if (endorsingRewards?.Count > 0)
                statistics.TotalCreated += endorsingRewards.Sum(x => x.Received);

            if (block.Activations != null)
                statistics.TotalActivated += block.Activations.Sum(x => x.Balance);

            if (block.DoubleBakings != null)
            {
                statistics.TotalBurned += block.DoubleBakings.Sum(x => x.OffenderLoss - x.AccuserReward);
                statistics.TotalFrozen -= block.DoubleBakings.Sum(x => x.OffenderLoss);
            }

            if (block.DoubleEndorsings != null)
            {
                statistics.TotalBurned += block.DoubleEndorsings.Sum(x => x.OffenderLoss - x.AccuserReward);
                statistics.TotalFrozen -= block.DoubleEndorsings.Sum(x => x.OffenderLoss);
            }

            if (block.DoublePreendorsings != null)
            {
                statistics.TotalBurned += block.DoublePreendorsings.Sum(x => x.OffenderLoss - x.AccuserReward);
                statistics.TotalFrozen -= block.DoublePreendorsings.Sum(x => x.OffenderLoss);
            }

            if (block.Originations != null)
            {
                var originations = block.Originations.Where(x => x.Status == OperationStatus.Applied);
                if (originations.Any())
                    statistics.TotalBurned += originations.Sum(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            }

            if (block.RegisterConstants != null)
            {
                var registerConstants = block.RegisterConstants.Where(x => x.Status == OperationStatus.Applied);
                if (registerConstants.Any())
                    statistics.TotalBurned += registerConstants.Sum(x => x.StorageFee ?? 0);
            }

            if (block.Transactions != null)
            {
                var transactions = block.Transactions.Where(x => x.Status == OperationStatus.Applied);

                if (transactions.Any())
                    statistics.TotalBurned += transactions.Sum(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));

                foreach (var tx in transactions.Where(x => x.Target?.Id == NullAddress.Id))
                    statistics.TotalBanished += tx.Amount;
            }

            if (block.Revelations != null)
            {
                var rewards = block.Revelations.Sum(x => x.Reward);
                statistics.TotalCreated += rewards;
            }

            if (block.VdfRevelationOps != null)
            {
                var rewards = block.VdfRevelationOps.Sum(x => x.Reward);
                statistics.TotalCreated += rewards;
            }

            if (block.Migrations != null && block.Migrations.Any(x => x.Kind == MigrationKind.Subsidy))
            {
                var subsidy = block.Migrations.Where(x => x.Kind == MigrationKind.Subsidy).Sum(x => x.BalanceChange);
                statistics.TotalCreated += subsidy;
            }

            if (block.TransferTicketOps != null)
            {
                var ops = block.TransferTicketOps.Where(x => x.Status == OperationStatus.Applied);
                if (ops.Any())
                    statistics.TotalBurned += ops.Sum(x => x.StorageFee ?? 0);
            }

            if (block.TxRollupCommitOps != null)
            {
                var ops = block.TxRollupCommitOps.Where(x => x.Status == OperationStatus.Applied);
                if (ops.Any())
                {
                    statistics.TotalBurned += ops.Sum(x => x.StorageFee ?? 0);
                    statistics.TotalRollupBonds += ops.Sum(x => x.Bond);
                }
            }

            if (block.TxRollupDispatchTicketsOps != null)
            {
                var ops = block.TxRollupDispatchTicketsOps.Where(x => x.Status == OperationStatus.Applied);
                if (ops.Any())
                    statistics.TotalBurned += ops.Sum(x => x.StorageFee ?? 0);
            }

            if (block.TxRollupFinalizeCommitmentOps != null)
            {
                var ops = block.TxRollupFinalizeCommitmentOps.Where(x => x.Status == OperationStatus.Applied);
                if (ops.Any())
                    statistics.TotalBurned += ops.Sum(x => x.StorageFee ?? 0);
            }

            if (block.TxRollupOriginationOps != null)
            {
                var ops = block.TxRollupOriginationOps.Where(x => x.Status == OperationStatus.Applied);
                if (ops.Any())
                    statistics.TotalBurned += ops.Sum(x => x.AllocationFee ?? 0);
            }

            if (block.TxRollupRejectionOps != null)
            {
                var ops = block.TxRollupRejectionOps.Where(x => x.Status == OperationStatus.Applied);
                if (ops.Any())
                {
                    statistics.TotalBurned += ops.Sum(x => x.StorageFee ?? 0);
                    statistics.TotalBurned += block.TxRollupRejectionOps.Sum(x => x.Loss - x.Reward);
                    statistics.TotalRollupBonds -= block.TxRollupRejectionOps.Sum(x => x.Loss);
                }
            }

            if (block.TxRollupRemoveCommitmentOps != null)
            {
                var ops = block.TxRollupRemoveCommitmentOps.Where(x => x.Status == OperationStatus.Applied);
                if (ops.Any())
                    statistics.TotalBurned += ops.Sum(x => x.StorageFee ?? 0);
            }

            if (block.TxRollupReturnBondOps != null)
            {
                var ops = block.TxRollupReturnBondOps.Where(x => x.Status == OperationStatus.Applied);
                if (ops.Any())
                {
                    statistics.TotalBurned += ops.Sum(x => x.StorageFee ?? 0);
                    statistics.TotalRollupBonds -= ops.Sum(x => x.Bond);
                }
            }

            if (block.TxRollupSubmitBatchOps != null)
            {
                var ops = block.TxRollupSubmitBatchOps.Where(x => x.Status == OperationStatus.Applied);
                if (ops.Any())
                    statistics.TotalBurned += ops.Sum(x => x.StorageFee ?? 0);
            }

            if (block.IncreasePaidStorageOps != null)
            {
                var ops = block.IncreasePaidStorageOps.Where(x => x.Status == OperationStatus.Applied);
                if (ops.Any())
                    statistics.TotalBurned += ops.Sum(x => x.StorageFee ?? 0);
            }

            if (block.UpdateConsensusKeyOps != null)
            {
                var ops = block.UpdateConsensusKeyOps.Where(x => x.Status == OperationStatus.Applied);
                if (ops.Any())
                    statistics.TotalBurned += ops.Sum(x => x.StorageFee ?? 0);
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
