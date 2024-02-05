using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class ProtoActivator : Proto1.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        // Activate weird delegates

        protected override async Task MigrateContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            var protocol = await Cache.Protocols.GetAsync(block.ProtoCode);

            var weirdDelegates = (await Db.Contracts
                .AsNoTracking()
                .Include(x => x.WeirdDelegate)
                .Where(x =>
                    x.DelegateId == null &&
                    x.WeirdDelegateId != null &&
                    x.WeirdDelegate.Balance > 0)
                .Select(x => new
                {
                    Contract = x,
                    Delegate = x.WeirdDelegate
                })
                .ToListAsync())
                .GroupBy(x => x.Delegate.Id);

            var activatedDelegates = new Dictionary<int, Data.Models.Delegate>(weirdDelegates.Count());

            foreach (var weirds in weirdDelegates)
            {
                var delegat = await UpgradeUser(weirds.First().Delegate, block.Level, protocol);
                activatedDelegates.Add(delegat.Id, delegat);

                delegat.MigrationsCount++;
                block.Operations |= Operations.Migrations;
                Db.MigrationOps.Add(new MigrationOperation
                {
                    Id = Cache.AppState.NextOperationId(),
                    Block = block,
                    Level = block.Level,
                    Timestamp = block.Timestamp,
                    Account = delegat,
                    Kind = MigrationKind.ActivateDelegate
                });
                state.MigrationOpsCount++;

                foreach (var weird in weirds)
                {
                    var delegator = weird.Contract;
                    if (delegator.DelegateId != null)
                        throw new Exception("migration error");

                    delegator.WeirdDelegate = null;

                    Db.TryAttach(delegator);
                    Cache.Accounts.Add(delegator);

                    delegator.WeirdDelegate = delegat;
                    delegator.Delegate = delegat;
                    delegator.DelegateId = delegat.Id;
                    delegator.DelegationLevel = delegator.FirstLevel;
                    delegator.Staked = true;

                    delegat.DelegatorsCount++;
                    delegat.StakingBalance += delegator.Balance;
                    delegat.DelegatedBalance += delegator.Balance;
                }
            }

            var ids = activatedDelegates.Keys.ToList();
            var weirdOriginations = await Db.OriginationOps
                .AsNoTracking()
                .Where(x => x.Contract != null && ids.Contains((int)x.Contract.WeirdDelegateId))
                .Select(x => new
                {
                    x.Contract.WeirdDelegateId,
                    Origination = x
                })
                .ToListAsync();

            foreach (var op in weirdOriginations)
            {
                var delegat = activatedDelegates[(int)op.WeirdDelegateId];

                Db.TryAttach(op.Origination);
                op.Origination.Delegate = delegat;
                if (delegat.Id != op.Origination.SenderId && delegat.Id != op.Origination.ManagerId) delegat.OriginationsCount++;
            }
        }

        protected override async Task RevertContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();

            var delegates = await Db.Delegates
                .AsNoTracking()
                .Include(x => x.DelegatedAccounts)
                .Where(x => x.ActivationLevel == block.Level)
                .ToListAsync();

            foreach (var delegat in delegates)
            {
                var delegators = delegat.DelegatedAccounts.ToList();
                foreach (var delegator in delegators)
                {
                    Db.TryAttach(delegator);
                    Cache.Accounts.Add(delegator);

                    (delegator as Contract).WeirdDelegate = null;
                    delegator.Delegate = null;
                    delegator.DelegateId = null;
                    delegator.DelegationLevel = null;
                    delegator.Staked = false;

                    delegat.DelegatorsCount--;
                    delegat.StakingBalance -= delegator.Balance;
                    delegat.DelegatedBalance -= delegator.Balance;
                }

                if (delegat.StakingBalance != delegat.Balance || delegat.DelegatorsCount > 0)
                    throw new Exception("migration error");

                var user = DowngradeDelegate(delegat);
                user.MigrationsCount--;

                foreach (var delegator in delegators)
                    (delegator as Contract).WeirdDelegate = user;
            }

            var migrationOps = await Db.MigrationOps
                .AsNoTracking()
                .Where(x => x.Kind == MigrationKind.ActivateDelegate)
                .ToListAsync();

            Db.MigrationOps.RemoveRange(migrationOps);
            Cache.AppState.ReleaseOperationId(migrationOps.Count);

            state.MigrationOpsCount -= migrationOps.Count;

            var ids = delegates.Select(x => x.Id).ToList();
            var weirdOriginations = await Db.OriginationOps
                .AsNoTracking()
                .Where(x => x.Contract != null && ids.Contains((int)x.Contract.WeirdDelegateId))
                .Select(x => new
                {
                    x.Contract.WeirdDelegateId,
                    Origination = x
                })
                .ToListAsync();

            foreach (var op in weirdOriginations)
            {
                var delegat = delegates.First(x => x.Id == op.WeirdDelegateId);

                Db.TryAttach(op.Origination);
                op.Origination.DelegateId = null;
                if (delegat.Id != op.Origination.SenderId && delegat.Id != op.Origination.ManagerId) delegat.OriginationsCount--;
            }
        }

        Task<Data.Models.Delegate> UpgradeUser(User user, int level, Protocol proto)
        {
            var delegat = new Data.Models.Delegate
            {
                ActivationLevel = level,
                Address = user.Address,
                FirstLevel = user.FirstLevel,
                LastLevel = user.LastLevel,
                Balance = user.Balance,
                Counter = user.Counter,
                DeactivationLevel = GracePeriod.Init(level, proto),
                Delegate = null,
                DelegateId = null,
                DelegationLevel = null,
                Id = user.Id,
                Activated = user.Activated,
                DelegationsCount = user.DelegationsCount,
                OriginationsCount = user.OriginationsCount,
                TransactionsCount = user.TransactionsCount,
                RevealsCount = user.RevealsCount,
                RegisterConstantsCount = user.RegisterConstantsCount,
                SetDepositsLimitsCount = user.SetDepositsLimitsCount,
                ContractsCount = user.ContractsCount,
                MigrationsCount = user.MigrationsCount,
                PublicKey = user.PublicKey,
                Revealed = user.Revealed,
                Staked = true,
                StakingBalance = user.Balance - user.UnstakedBalance,
                DelegatedBalance = 0,
                Type = AccountType.Delegate,
                StakedBalance = user.StakedBalance,
                StakedPseudotokens = user.StakedPseudotokens,
                UnstakedBalance = user.UnstakedBalance,
                UnstakedBakerId = user.UnstakedBakerId,
                StakingOpsCount = user.StakingOpsCount,
                TotalStakedBalance = user.StakedBalance,
                ActiveTokensCount = user.ActiveTokensCount,
                TokenBalancesCount = user.TokenBalancesCount,
                TokenTransfersCount = user.TokenTransfersCount,
                ActiveTicketsCount = user.ActiveTicketsCount,
                TicketBalancesCount = user.TicketBalancesCount,
                TicketTransfersCount = user.TicketTransfersCount,
                TransferTicketCount = user.TransferTicketCount,
                TxRollupCommitCount = user.TxRollupCommitCount,
                TxRollupDispatchTicketsCount = user.TxRollupDispatchTicketsCount,
                TxRollupFinalizeCommitmentCount = user.TxRollupFinalizeCommitmentCount,
                TxRollupOriginationCount = user.TxRollupOriginationCount,
                TxRollupRejectionCount = user.TxRollupRejectionCount,
                TxRollupRemoveCommitmentCount = user.TxRollupRemoveCommitmentCount,
                TxRollupReturnBondCount = user.TxRollupReturnBondCount,
                TxRollupSubmitBatchCount = user.TxRollupSubmitBatchCount,
                IncreasePaidStorageCount = user.IncreasePaidStorageCount,
                UpdateConsensusKeyCount = user.UpdateConsensusKeyCount,
                DrainDelegateCount = user.DrainDelegateCount,
                RollupBonds = user.RollupBonds,
                RollupsCount = user.RollupsCount,
                SmartRollupBonds = user.SmartRollupBonds,
                SmartRollupsCount = user.SmartRollupsCount,
                SmartRollupAddMessagesCount = user.SmartRollupAddMessagesCount,
                SmartRollupCementCount = user.SmartRollupCementCount,
                SmartRollupExecuteCount = user.SmartRollupExecuteCount,
                SmartRollupOriginateCount = user.SmartRollupOriginateCount,
                SmartRollupPublishCount = user.SmartRollupPublishCount,
                SmartRollupRecoverBondCount = user.SmartRollupRecoverBondCount,
                SmartRollupRefuteCount = user.SmartRollupRefuteCount,
                RefutationGamesCount = user.RefutationGamesCount,
                ActiveRefutationGamesCount = user.ActiveRefutationGamesCount
            };

            Db.Entry(user).State = EntityState.Detached;
            Db.Entry(delegat).State = EntityState.Modified;
            Cache.Accounts.Add(delegat);

            return Task.FromResult(delegat);
        }

        User DowngradeDelegate(Data.Models.Delegate delegat)
        {
            var user = new User
            {
                Address = delegat.Address,
                FirstLevel = delegat.FirstLevel,
                LastLevel = delegat.LastLevel,
                Balance = delegat.Balance,
                Counter = delegat.Counter,
                Delegate = null,
                DelegateId = null,
                DelegationLevel = null,
                StakedBalance = delegat.StakedBalance,
                StakedPseudotokens = delegat.StakedPseudotokens,
                UnstakedBalance = delegat.UnstakedBalance,
                UnstakedBakerId = delegat.UnstakedBakerId,
                StakingOpsCount = delegat.StakingOpsCount,
                Id = delegat.Id,
                Activated = delegat.Activated,
                DelegationsCount = delegat.DelegationsCount,
                OriginationsCount = delegat.OriginationsCount,
                TransactionsCount = delegat.TransactionsCount,
                RevealsCount = delegat.RevealsCount,
                RegisterConstantsCount = delegat.RegisterConstantsCount,
                SetDepositsLimitsCount = delegat.SetDepositsLimitsCount,
                ContractsCount = delegat.ContractsCount,
                MigrationsCount = delegat.MigrationsCount,
                PublicKey = delegat.PublicKey,
                Revealed = delegat.Revealed,
                Staked = false,
                Type = AccountType.User,
                ActiveTokensCount = delegat.ActiveTokensCount,
                TokenBalancesCount = delegat.TokenBalancesCount,
                TokenTransfersCount = delegat.TokenTransfersCount,
                ActiveTicketsCount = delegat.ActiveTicketsCount,
                TicketBalancesCount = delegat.TicketBalancesCount,
                TicketTransfersCount = delegat.TicketTransfersCount,
                TransferTicketCount = delegat.TransferTicketCount,
                TxRollupCommitCount = delegat.TxRollupCommitCount,
                TxRollupDispatchTicketsCount = delegat.TxRollupDispatchTicketsCount,
                TxRollupFinalizeCommitmentCount = delegat.TxRollupFinalizeCommitmentCount,
                TxRollupOriginationCount = delegat.TxRollupOriginationCount,
                TxRollupRejectionCount = delegat.TxRollupRejectionCount,
                TxRollupRemoveCommitmentCount = delegat.TxRollupRemoveCommitmentCount,
                TxRollupReturnBondCount = delegat.TxRollupReturnBondCount,
                TxRollupSubmitBatchCount = delegat.TxRollupSubmitBatchCount,
                IncreasePaidStorageCount = delegat.IncreasePaidStorageCount,
                UpdateConsensusKeyCount = delegat.UpdateConsensusKeyCount,
                DrainDelegateCount = delegat.DrainDelegateCount,
                RollupBonds = delegat.RollupBonds,
                RollupsCount = delegat.RollupsCount,
                SmartRollupBonds = delegat.SmartRollupBonds,
                SmartRollupsCount = delegat.SmartRollupsCount,
                SmartRollupAddMessagesCount = delegat.SmartRollupAddMessagesCount,
                SmartRollupCementCount = delegat.SmartRollupCementCount,
                SmartRollupExecuteCount = delegat.SmartRollupExecuteCount,
                SmartRollupOriginateCount = delegat.SmartRollupOriginateCount,
                SmartRollupPublishCount = delegat.SmartRollupPublishCount,
                SmartRollupRecoverBondCount = delegat.SmartRollupRecoverBondCount,
                SmartRollupRefuteCount = delegat.SmartRollupRefuteCount,
                RefutationGamesCount = delegat.RefutationGamesCount,
                ActiveRefutationGamesCount = delegat.ActiveRefutationGamesCount
            };

            Db.Entry(delegat).State = EntityState.Detached;
            Db.Entry(user).State = EntityState.Modified;
            Cache.Accounts.Add(user);

            return user;
        }
    }
}
