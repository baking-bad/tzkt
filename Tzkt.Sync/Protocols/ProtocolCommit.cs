using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols
{
    public abstract class ProtocolCommit(ProtocolHandler protocol)
    {
        protected readonly TzktContext Db = protocol.Db;
        protected readonly CacheService Cache = protocol.Cache;
        protected readonly ProtocolHandler Proto = protocol;
        protected readonly BlockContext Context = protocol.Context;
        protected readonly ILogger Logger = protocol.Logger;

        protected Data.Models.Delegate RegisterBaker(User user, Protocol? protocol = null)
        {
            var baker = new Data.Models.Delegate
            {
                ActivationLevel = Context.Block.Level,
                DeactivationLevel = GracePeriod.Init(Context.Block.Level, protocol ?? Context.Protocol),
                Address = user.Address,
                FirstLevel = user.FirstLevel,
                LastLevel = user.LastLevel,
                Balance = user.Balance,
                Counter = user.Counter,
                DelegateId = null,
                DelegationLevel = null,
                Id = user.Id,
                Index = user.Index,
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
                OwnDelegatedBalance = user.Balance - user.UnstakedBalance,
                StakedPseudotokens = user.StakedPseudotokens,
                UnstakedBalance = user.UnstakedBalance,
                UnstakedBakerId = user.UnstakedBakerId,
                StakingOpsCount = user.StakingOpsCount,
                ExternalDelegatedBalance = 0,
                MinTotalDelegated = long.MaxValue,
                MinTotalDelegatedLevel = 0,
                Type = AccountType.Delegate,
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

            UpdateBakerPower(baker);

            Cache.Statistics.Current.TotalOwnDelegated += baker.OwnDelegatedBalance;
            Cache.Statistics.Current.TotalBakers++;

            var isAdded = Db.Entry(user).State == EntityState.Added;
            Db.Entry(user).State = EntityState.Detached;
            Db.Entry(baker).State = isAdded ? EntityState.Added : EntityState.Modified;
            Cache.Accounts.Add(baker);

            return baker;
        }

        protected User UnregisterBaker(Data.Models.Delegate baker)
        {
            var user = new User
            {
                Address = baker.Address,
                FirstLevel = baker.FirstLevel,
                LastLevel = baker.LastLevel,
                Balance = baker.Balance,
                Counter = baker.Counter,
                DelegateId = null,
                DelegationLevel = null,
                StakedPseudotokens = baker.StakedPseudotokens,
                UnstakedBalance = baker.UnstakedBalance,
                UnstakedBakerId = baker.UnstakedBakerId,
                StakingOpsCount = baker.StakingOpsCount,
                Id = baker.Id,
                Index = baker.Index,
                ActivationsCount = baker.ActivationsCount,
                DelegationsCount = baker.DelegationsCount,
                OriginationsCount = baker.OriginationsCount,
                TransactionsCount = baker.TransactionsCount,
                RevealsCount = baker.RevealsCount,
                RegisterConstantsCount = baker.RegisterConstantsCount,
                SetDepositsLimitsCount = baker.SetDepositsLimitsCount,
                ContractsCount = baker.ContractsCount,
                MigrationsCount = baker.MigrationsCount,
                PublicKey = baker.PublicKey,
                Revealed = baker.Revealed,
                Staked = false,
                Type = AccountType.User,
                ActiveTokensCount = baker.ActiveTokensCount,
                TokenBalancesCount = baker.TokenBalancesCount,
                TokenTransfersCount = baker.TokenTransfersCount,
                ActiveTicketsCount = baker.ActiveTicketsCount,
                TicketBalancesCount = baker.TicketBalancesCount,
                TicketTransfersCount = baker.TicketTransfersCount,
                TransferTicketCount = baker.TransferTicketCount,
                TxRollupCommitCount = baker.TxRollupCommitCount,
                TxRollupDispatchTicketsCount = baker.TxRollupDispatchTicketsCount,
                TxRollupFinalizeCommitmentCount = baker.TxRollupFinalizeCommitmentCount,
                TxRollupOriginationCount = baker.TxRollupOriginationCount,
                TxRollupRejectionCount = baker.TxRollupRejectionCount,
                TxRollupRemoveCommitmentCount = baker.TxRollupRemoveCommitmentCount,
                TxRollupReturnBondCount = baker.TxRollupReturnBondCount,
                TxRollupSubmitBatchCount = baker.TxRollupSubmitBatchCount,
                IncreasePaidStorageCount = baker.IncreasePaidStorageCount,
                UpdateSecondaryKeyCount = baker.UpdateSecondaryKeyCount,
                DrainDelegateCount = baker.DrainDelegateCount,
                RollupBonds = baker.RollupBonds,
                RollupsCount = baker.RollupsCount,
                SmartRollupBonds = baker.SmartRollupBonds,
                SmartRollupsCount = baker.SmartRollupsCount,
                SmartRollupAddMessagesCount = baker.SmartRollupAddMessagesCount,
                SmartRollupCementCount = baker.SmartRollupCementCount,
                SmartRollupExecuteCount = baker.SmartRollupExecuteCount,
                SmartRollupOriginateCount = baker.SmartRollupOriginateCount,
                SmartRollupPublishCount = baker.SmartRollupPublishCount,
                SmartRollupRecoverBondCount = baker.SmartRollupRecoverBondCount,
                SmartRollupRefuteCount = baker.SmartRollupRefuteCount,
                SetDelegateParametersOpsCount = baker.SetDelegateParametersOpsCount,
                DalPublishCommitmentOpsCount = baker.DalPublishCommitmentOpsCount,
                RefutationGamesCount = baker.RefutationGamesCount,
                ActiveRefutationGamesCount = baker.ActiveRefutationGamesCount,
                StakingUpdatesCount = baker.StakingUpdatesCount
            };

            var isAdded = Db.Entry(baker).State == EntityState.Added;
            Db.Entry(baker).State = EntityState.Detached;
            Db.Entry(user).State = isAdded ? EntityState.Added : EntityState.Modified;
            Cache.Accounts.Add(user);

            return user;
        }

        protected async Task ActivateBaker(Data.Models.Delegate baker)
        {
            baker.Staked = true;

            foreach (var delegator in await Db.Accounts.Where(x => x.DelegateId == baker.Id).ToListAsync())
            {
                Cache.Accounts.Add(delegator);
                delegator.LastLevel = Context.Block.Level;
                delegator.Staked = true;
            }

            UpdateBakerPower(baker);

            Cache.Statistics.Current.TotalOwnStaked += baker.OwnStakedBalance;
            Cache.Statistics.Current.TotalExternalStaked += baker.ExternalStakedBalance;
            Cache.Statistics.Current.TotalOwnDelegated += baker.OwnDelegatedBalance;
            Cache.Statistics.Current.TotalExternalDelegated += baker.ExternalDelegatedBalance;

            Cache.Statistics.Current.TotalBakers++;
            Cache.Statistics.Current.TotalStakers += baker.StakersCount;
            Cache.Statistics.Current.TotalDelegators += baker.DelegatorsCount;
        }

        protected async Task DeactivateBaker(Data.Models.Delegate baker)
        {
            baker.Staked = false;

            foreach (var delegator in await Db.Accounts.Where(x => x.DelegateId == baker.Id).ToListAsync())
            {
                Cache.Accounts.Add(delegator);
                delegator.LastLevel = Context.Block.Level;
                delegator.Staked = false;
            }

            UpdateBakerPower(baker);

            Cache.Statistics.Current.TotalOwnStaked -= baker.OwnStakedBalance;
            Cache.Statistics.Current.TotalExternalStaked -= baker.ExternalStakedBalance;
            Cache.Statistics.Current.TotalOwnDelegated -= baker.OwnDelegatedBalance;
            Cache.Statistics.Current.TotalExternalDelegated -= baker.ExternalDelegatedBalance;

            Cache.Statistics.Current.TotalBakers--;
            Cache.Statistics.Current.TotalStakers -= baker.StakersCount;
            Cache.Statistics.Current.TotalDelegators -= baker.DelegatorsCount;
        }

        protected void UpdateBakersPower()
        {
            foreach (var baker in Cache.Accounts.GetDelegates())
            {
                Db.TryAttach(baker);
                UpdateBakerPower(baker);
            }
        }

        protected void UpdateBakerPower(Data.Models.Delegate baker)
        {
            Cache.Statistics.Current.TotalBakingPower -= baker.BakingPower;
            Cache.Statistics.Current.TotalVotingPower -= baker.VotingPower;
            
            baker.BakingPower = Proto.Helpers.BakingPower(baker);
            baker.VotingPower = Proto.Helpers.VotingPower(baker);

            Cache.Statistics.Current.TotalBakingPower += baker.BakingPower;
            Cache.Statistics.Current.TotalVotingPower += baker.VotingPower;
        }

        protected void RevertBakersPower()
        {
            foreach (var baker in Cache.Accounts.GetDelegates())
            {
                Db.TryAttach(baker);
                RevertBakerPower(baker);
            }
        }

        protected void RevertBakerPower(Data.Models.Delegate baker)
        {
            baker.BakingPower = Proto.Helpers.BakingPower(baker);
            baker.VotingPower = Proto.Helpers.VotingPower(baker);
        }

        protected void ReceiveLockedRewards(Data.Models.Delegate baker, long amount)
        {
            baker.Balance += amount;
            UpdateBakerPower(baker);
        }

        protected void RevertReceiveLockedRewards(Data.Models.Delegate baker, long amount)
        {
            baker.Balance -= amount;
            RevertBakerPower(baker);
        }

        protected void BurnLockedRewards(Data.Models.Delegate baker, long amount)
        {
            baker.Balance -= amount;
            UpdateBakerPower(baker);
        }

        protected void RevertBurnLockedRewards(Data.Models.Delegate baker, long amount)
        {
            baker.Balance += amount;
            RevertBakerPower(baker);
        }

        protected void UnlockRewards(Data.Models.Delegate baker, long amount)
        {
            baker.OwnDelegatedBalance += amount;
            UpdateBakerPower(baker);

            if (baker.Staked)
                Cache.Statistics.Current.TotalOwnDelegated += amount;
        }

        protected void RevertUnlockRewards(Data.Models.Delegate baker, long amount)
        {
            baker.OwnDelegatedBalance -= amount;
            RevertBakerPower(baker);
        }

        protected void PayFee(Account account, long bakerFee)
        {
            Spend(account, bakerFee);

            Context.Block.Fees += bakerFee;

            Context.Proposer.Balance += bakerFee;
            Context.Proposer.OwnDelegatedBalance += bakerFee;
            UpdateBakerPower(Context.Proposer);

            if (Context.Proposer.Staked)
                Cache.Statistics.Current.TotalOwnDelegated += bakerFee;
        }

        protected void RevertPayFee(Account account, long bakerFee)
        {
            RevertSpend(account, bakerFee);

            Context.Proposer.Balance -= bakerFee;
            Context.Proposer.OwnDelegatedBalance -= bakerFee;
            RevertBakerPower(Context.Proposer);
        }

        protected void Spend(Account account, long amount)
        {
            var baker = Cache.Accounts.GetDelegate(account.DelegateId) ?? account as Data.Models.Delegate;
            Db.TryAttach(baker);

            Spend(account, baker, amount);
        }

        protected void Spend(Account account, Data.Models.Delegate? baker, long amount)
        {
            account.Balance -= amount;

            if (baker != null)
            {
                if (baker == account)
                {
                    baker.OwnDelegatedBalance -= amount;
                    if (baker.Staked)
                        Cache.Statistics.Current.TotalOwnDelegated -= amount;
                }
                else
                {
                    baker.ExternalDelegatedBalance -= amount;
                    if (baker.Staked)
                        Cache.Statistics.Current.TotalExternalDelegated -= amount;
                }

                UpdateBakerPower(baker);
            }
        }

        protected void RevertSpend(Account account, long amount)
        {
            var baker = Cache.Accounts.GetDelegate(account.DelegateId) ?? account as Data.Models.Delegate;
            Db.TryAttach(baker);

            RevertSpend(account, baker, amount);
        }

        protected void RevertSpend(Account account, Data.Models.Delegate? baker, long amount)
        {
            account.Balance += amount;

            if (baker != null)
            {
                if (baker == account)
                    baker.OwnDelegatedBalance += amount;
                else
                    baker.ExternalDelegatedBalance += amount;

                RevertBakerPower(baker);
            }
        }

        protected void Receive(Account account, long amount)
        {
            var baker = Cache.Accounts.GetDelegate(account.DelegateId) ?? account as Data.Models.Delegate;
            Db.TryAttach(baker);

            Receive(account, baker, amount);
        }

        protected void Receive(Account account, Data.Models.Delegate? baker, long amount)
        {
            account.Balance += amount;

            if (baker != null)
            {
                if (baker == account)
                {
                    baker.OwnDelegatedBalance += amount;
                    if (baker.Staked)
                        Cache.Statistics.Current.TotalOwnDelegated += amount;
                }
                else
                {
                    baker.ExternalDelegatedBalance += amount;
                    if (baker.Staked)
                        Cache.Statistics.Current.TotalExternalDelegated += amount;
                }

                UpdateBakerPower(baker);
            }
        }

        protected void RevertReceive(Account account, long amount)
        {
            var baker = Cache.Accounts.GetDelegate(account.DelegateId) ?? account as Data.Models.Delegate;
            Db.TryAttach(baker);

            RevertReceive(account, baker, amount);
        }

        protected void RevertReceive(Account account, Data.Models.Delegate? baker, long amount)
        {
            account.Balance -= amount;

            if (baker != null)
            {
                if (baker == account)
                    baker.OwnDelegatedBalance -= amount;
                else
                    baker.ExternalDelegatedBalance -= amount;

                RevertBakerPower(baker);
            }
        }

        protected void ReceiveRewards(Data.Models.Delegate baker, long delegated, long stakedOwn, long stakedEdge, long stakedShared)
        {
            baker.Balance += delegated + stakedOwn + stakedEdge;
            baker.OwnDelegatedBalance += delegated;
            baker.OwnStakedBalance += stakedOwn + stakedEdge;
            baker.ExternalStakedBalance += stakedShared;
            UpdateBakerPower(baker);

            if (baker.Staked)
            {
                Cache.Statistics.Current.TotalOwnDelegated += delegated;
                Cache.Statistics.Current.TotalOwnStaked += stakedOwn + stakedEdge;
                Cache.Statistics.Current.TotalExternalStaked += stakedShared;
            }
        }

        protected void RevertReceiveRewards(Data.Models.Delegate baker, long delegated, long stakedOwn, long stakedEdge, long stakedShared)
        {
            baker.Balance -= delegated + stakedOwn + stakedEdge;
            baker.OwnDelegatedBalance -= delegated;
            baker.OwnStakedBalance -= stakedOwn + stakedEdge;
            baker.ExternalStakedBalance -= stakedShared;
            RevertBakerPower(baker);
        }

        protected void Delegate(Account delegator, Data.Models.Delegate baker, int delegationLevel)
        {
            var amount = delegator.Balance - ((delegator as User)?.UnstakedBalance ?? 0);

            delegator.DelegateId = baker.Id;
            delegator.DelegationLevel = delegationLevel;
            delegator.Staked = baker.Staked;

            baker.DelegatorsCount++;
            baker.ExternalDelegatedBalance += amount;

            UpdateBakerPower(baker);

            if (baker.Staked)
            {
                Cache.Statistics.Current.TotalExternalDelegated += amount;
                Cache.Statistics.Current.TotalDelegators++;
            }
        }

        protected void Undelegate(Account delegator, Data.Models.Delegate baker)
        {
            var amount = delegator.Balance - ((delegator as User)?.UnstakedBalance ?? 0);

            delegator.DelegateId = null;
            delegator.DelegationLevel = null;
            delegator.Staked = false;

            baker.DelegatorsCount--;
            baker.ExternalDelegatedBalance -= amount;

            UpdateBakerPower(baker);

            if (baker.Staked)
            {
                Cache.Statistics.Current.TotalExternalDelegated -= amount;
                Cache.Statistics.Current.TotalDelegators--;
            }
        }
    }
}
