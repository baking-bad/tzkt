﻿using Microsoft.EntityFrameworkCore;
using Mvkt.Data;
using Mvkt.Data.Models;
using Mvkt.Data.Models.Base;

namespace Mvkt.Sync.Tests.Database
{
    internal class StatisticsTests
    {
        public static async Task RunAsync(MvktContext db)
        {
            var stats = await db.Statistics.OrderByDescending(x => x.Level).FirstAsync();

            var totalCommitments = await db.Commitments.SumAsync(x => x.Balance);

            if (stats.TotalCommitments != totalCommitments)
                throw new Exception("Invalid Statistics.TotalCommitments");

            var totalActivated = await db.ActivationOps.SumAsync(x => x.Balance);

            if (stats.TotalActivated != totalActivated)
                throw new Exception("Invalid Statistics.TotalActivated");

            var totalCreated = await db.Blocks.SumAsync(x => x.RewardLiquid + x.RewardStakedOwn + x.RewardStakedShared + x.BonusLiquid + x.BonusStakedOwn + x.BonusStakedShared);
            totalCreated += await db.EndorsementOps.SumAsync(x => x.Reward);
            totalCreated += await db.EndorsingRewardOps.SumAsync(x => x.RewardLiquid + x.RewardStakedOwn + x.RewardStakedShared);
            totalCreated += await db.NonceRevelationOps.SumAsync(x => x.RewardLiquid + x.RewardStakedOwn + x.RewardStakedShared);
            totalCreated += await db.VdfRevelationOps.SumAsync(x => x.RewardLiquid + x.RewardStakedOwn + x.RewardStakedShared);
            totalCreated += await db.MigrationOps.Where(x => x.Kind != MigrationKind.Bootstrap).SumAsync(x => x.BalanceChange);

            if (stats.TotalCreated != totalCreated)
                throw new Exception("Invalid Statistics.TotalCreated");

            var totalBurned = await db.DoubleBakingOps.SumAsync(x => x.LostStaked + x.LostUnstaked + x.LostExternalStaked + x.LostExternalUnstaked - x.Reward);
            totalBurned += await db.DoubleEndorsingOps.SumAsync(x => x.LostStaked + x.LostUnstaked + x.LostExternalStaked + x.LostExternalUnstaked - x.Reward);
            totalBurned += await db.DoublePreendorsingOps.SumAsync(x => x.LostStaked + x.LostUnstaked + x.LostExternalStaked + x.LostExternalUnstaked - x.Reward);
            totalBurned += await db.RevelationPenaltyOps.SumAsync(x => x.Loss);
            totalBurned += await db.DelegationOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.IncreasePaidStorageOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.OriginationOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.RegisterConstantOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.RevealOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.SetDepositsLimitOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.TransactionOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.TransferTicketOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.TxRollupCommitOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.TxRollupDispatchTicketsOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.TxRollupFinalizeCommitmentOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.TxRollupOriginationOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.TxRollupRejectionOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0) + x.Loss);
            totalBurned += await db.TxRollupRemoveCommitmentOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.TxRollupReturnBondOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.TxRollupSubmitBatchOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.UpdateConsensusKeyOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.SmartRollupAddMessagesOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.SmartRollupCementOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.SmartRollupExecuteOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.SmartRollupOriginateOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.SmartRollupPublishOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.SmartRollupRecoverBondOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.SmartRollupRefuteOps.Where(x => x.Status == OperationStatus.Applied).SumAsync(x => (x.StorageFee ?? 0) + (x.AllocationFee ?? 0));
            totalBurned += await db.RefutationGames.Where(x => x.InitiatorLoss != null || x.OpponentLoss != null).SumAsync(x => (x.InitiatorLoss ?? 0) + (x.OpponentLoss ?? 0) - (x.InitiatorReward ?? 0) - (x.OpponentReward ?? 0));

            if (stats.TotalBurned != totalBurned)
                throw new Exception("Invalid Statistics.TotalBurned");

            var totalLost = await db.DoubleBakingOps.SumAsync(x => x.RoundingLoss);
            totalLost += await db.DoubleEndorsingOps.SumAsync(x => x.RoundingLoss);
            totalLost += await db.DoublePreendorsingOps.SumAsync(x => x.RoundingLoss);

            if (stats.TotalLost != totalLost)
                throw new Exception("Invalid Statistics.TotalLost");

            var totalBanished = await db.Accounts.Where(x => x.Id == 1).SumAsync(x => x.Balance);

            if (stats.TotalBanished != totalBanished)
                throw new Exception("Invalid Statistics.TotalBanished");

            var totalFrozen = await db.Delegates.SumAsync(x => x.StakedBalance + x.ExternalStakedBalance);

            if (stats.TotalFrozen != totalFrozen)
                throw new Exception("Invalid Statistics.TotalFrozen");

            var totalRollupBonds = await db.Accounts.Where(x => x.Type != AccountType.Rollup).SumAsync(x => x.RollupBonds);
            var totalRollupBonds2 = await db.Accounts.Where(x => x.Type == AccountType.Rollup).SumAsync(x => x.RollupBonds);

            if (stats.TotalRollupBonds != totalRollupBonds || stats.TotalRollupBonds != totalRollupBonds2)
                throw new Exception("Invalid Statistics.TotalRollupBonds");

            var totalSmartRollupBonds = await db.Accounts.Where(x => x.Type != AccountType.SmartRollup).SumAsync(x => x.SmartRollupBonds);
            var totalSmartRollupBonds2 = await db.Accounts.Where(x => x.Type == AccountType.SmartRollup).SumAsync(x => x.SmartRollupBonds);

            if (stats.TotalSmartRollupBonds != totalSmartRollupBonds || stats.TotalSmartRollupBonds != totalSmartRollupBonds2)
                throw new Exception("Invalid Statistics.TotalSmartRollupBonds");

            var totalBalances = await db.Accounts.SumAsync(x => x.Balance);
            var pendingRewards = await db.Delegates.SumAsync(x => x.ExternalStakedBalance) - await db.Users.Where(x => x.Type == AccountType.User).SumAsync(x => x.StakedBalance);
            var totalBalancesStats = stats.TotalBootstrapped + stats.TotalActivated + stats.TotalCreated - stats.TotalBurned;

            if (totalBalancesStats != totalBalances + pendingRewards)
                throw new Exception("Invalid Statistics.TotalBalances");
        }
    }
}
