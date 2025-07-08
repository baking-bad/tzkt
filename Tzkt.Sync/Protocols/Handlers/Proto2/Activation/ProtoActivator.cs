using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto2
{
    class ProtoActivator(ProtocolHandler proto) : Proto1.ProtoActivator(proto)
    {
        protected override async Task MigrateContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            var protocol = await Cache.Protocols.GetAsync(block.ProtoCode);
            
            var weirdDelegates = (await Db.OriginationOps
                .AsNoTracking()
                .Join(Db.Users, x => x.DelegateId, x => x.Id, (op, delegat) => new { op, delegat })
                .Join(Db.Accounts, x => x.op.ContractId, x => x.Id, (opDelegat, contract) => new { opDelegat.op, opDelegat.delegat, contract })
                .Where(x =>
                    x.op.Status == OperationStatus.Applied &&
                    x.op.DelegateId != null &&
                    x.delegat.Type != AccountType.Delegate &&
                    x.delegat.Balance > 0 &&
                    x.contract.DelegateId == null)
                .Select(x => new
                {
                    Contract = x.contract,
                    WeirdDelegate = x.delegat
                })
                .ToListAsync())
                .GroupBy(x => x.WeirdDelegate.Id);

            var activatedDelegates = new Dictionary<int, Data.Models.Delegate>(weirdDelegates.Count());

            Db.TryAttach(block);
            Db.TryAttach(state);

            foreach (var weirds in weirdDelegates)
            {
                var delegat = UpgradeUser(weirds.First().WeirdDelegate, block.Level, protocol);
                activatedDelegates.Add(delegat.Id, delegat);

                delegat.MigrationsCount++;
                delegat.LastLevel = block.Level;

                block.Operations |= Operations.Migrations;

                var migration = new MigrationOperation
                {
                    Id = Cache.AppState.NextOperationId(),
                    Level = block.Level,
                    Timestamp = block.Timestamp,
                    AccountId = delegat.Id,
                    Kind = MigrationKind.ActivateDelegate
                };
                Db.MigrationOps.Add(migration);
                Context.MigrationOps.Add(migration);
                
                state.MigrationOpsCount++;

                foreach (var weird in weirds)
                {
                    var delegator = weird.Contract;
                    if (delegator.DelegateId != null)
                        throw new Exception("migration error");

                    Db.TryAttach(delegator);
                    Cache.Accounts.Add(delegator);

                    delegator.DelegateId = delegat.Id;
                    delegator.DelegationLevel = delegator.FirstLevel;
                    delegator.Staked = true;

                    delegat.DelegatorsCount++;
                    delegat.StakingBalance += delegator.Balance;
                    delegat.DelegatedBalance += delegator.Balance;
                }
            }
        }

        protected override async Task RevertContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();

            var delegates = await Db.Delegates
                .AsNoTracking()
                .GroupJoin(Db.Accounts, x => x.Id, x => x.DelegateId, (baker, delegators) => new { baker, delegators })
                .Where(x => x.baker.ActivationLevel == block.Level)
                .ToListAsync();

            foreach (var row in delegates)
            {
                foreach (var delegator in row.delegators)
                {
                    Db.TryAttach(delegator);
                    Cache.Accounts.Add(delegator);

                    delegator.DelegateId = null;
                    delegator.DelegationLevel = null;
                    delegator.Staked = false;

                    row.baker.DelegatorsCount--;
                    row.baker.StakingBalance -= delegator.Balance;
                    row.baker.DelegatedBalance -= delegator.Balance;
                }

                if (row.baker.StakingBalance != row.baker.Balance || row.baker.DelegatorsCount > 0)
                    throw new Exception("migration error");

                var user = DowngradeDelegate(row.baker);
                user.MigrationsCount--;
            }

            var migrationOps = await Db.MigrationOps
                .AsNoTracking()
                .Where(x => x.Kind == MigrationKind.ActivateDelegate)
                .ToListAsync();

            Db.MigrationOps.RemoveRange(migrationOps);
            Cache.AppState.ReleaseOperationId(migrationOps.Count);

            state.MigrationOpsCount -= migrationOps.Count;
        }

        Data.Models.Delegate UpgradeUser(User user, int level, Protocol proto)
        {
            var delegat = new Data.Models.Delegate
            {
                Id = user.Id,
                ActivationLevel = level,
                Address = user.Address,
                FirstLevel = user.FirstLevel,
                LastLevel = user.LastLevel,
                Balance = user.Balance,
                Counter = user.Counter,
                DeactivationLevel = GracePeriod.Init(level, proto),
                DelegateId = null,
                DelegationLevel = null,
                ActivationsCount = user.ActivationsCount,
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
                MinTotalDelegated = long.MaxValue,
                MinTotalDelegatedLevel = 0,
                Type = AccountType.Delegate,
                StakedPseudotokens = user.StakedPseudotokens,
                UnstakedBalance = user.UnstakedBalance,
                UnstakedBakerId = user.UnstakedBakerId,
                StakingOpsCount = user.StakingOpsCount,
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
                UpdateSecondaryKeyCount = user.UpdateSecondaryKeyCount,
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
                DalPublishCommitmentOpsCount = user.DalPublishCommitmentOpsCount,
                SetDelegateParametersOpsCount = user.SetDelegateParametersOpsCount,
                RefutationGamesCount = user.RefutationGamesCount,
                ActiveRefutationGamesCount = user.ActiveRefutationGamesCount,
                StakingUpdatesCount = user.StakingUpdatesCount
            };

            Db.Entry(user).State = EntityState.Detached;
            Db.Entry(delegat).State = EntityState.Modified;
            Cache.Accounts.Add(delegat);

            return delegat;
        }

        User DowngradeDelegate(Data.Models.Delegate delegat)
        {
            var user = new User
            {
                Id = delegat.Id,
                Address = delegat.Address,
                Type = AccountType.User,
                FirstLevel = delegat.FirstLevel,
                LastLevel = delegat.LastLevel,
                Balance = delegat.Balance,
                Counter = delegat.Counter,
                DelegateId = null,
                DelegationLevel = null,
                StakedPseudotokens = delegat.StakedPseudotokens,
                UnstakedBalance = delegat.UnstakedBalance,
                UnstakedBakerId = delegat.UnstakedBakerId,
                StakingOpsCount = delegat.StakingOpsCount,
                ActivationsCount = delegat.ActivationsCount,
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
                UpdateSecondaryKeyCount = delegat.UpdateSecondaryKeyCount,
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
                DalPublishCommitmentOpsCount = delegat.DalPublishCommitmentOpsCount,
                SetDelegateParametersOpsCount = delegat.SetDelegateParametersOpsCount,
                RefutationGamesCount = delegat.RefutationGamesCount,
                ActiveRefutationGamesCount = delegat.ActiveRefutationGamesCount,
                StakingUpdatesCount = delegat.StakingUpdatesCount
            };

            Db.Entry(delegat).State = EntityState.Detached;
            Db.Entry(user).State = EntityState.Modified;
            Cache.Accounts.Add(user);

            return user;
        }
    }
}
