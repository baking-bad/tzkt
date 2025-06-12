using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto1
{
    class DelegationsCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));
            var prevDelegate = sender.DelegateId is int senderDelegateId
                ? await Cache.Accounts.GetAsync(senderDelegateId) as Data.Models.Delegate
                : sender as Data.Models.Delegate;
            var newDelegate = content.OptionalString("delegate") is string _delegateAddress
                ? await Cache.Accounts.GetOrCreateAsync(_delegateAddress)
                : null;

            var result = content.Required("metadata").Required("operation_result");

            var delegation = new DelegationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = Context.Block.Level,
                Timestamp = Context.Block.Timestamp,
                OpHash = op.RequiredString("hash"),
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                SenderId = sender.Id,
                DelegateId = newDelegate?.Id,
                PrevDelegateId = prevDelegate?.Id,
                Amount = sender.Balance - content.RequiredInt64("fee"),
                Status = result.RequiredString("status") switch
                {
                    "applied" => OperationStatus.Applied,
                    "backtracked" => OperationStatus.Backtracked,
                    "failed" => OperationStatus.Failed,
                    "skipped" => OperationStatus.Skipped,
                    _ => throw new NotImplementedException()
                },
                Errors = result.TryGetProperty("errors", out var errors)
                    ? OperationErrors.Parse(content, errors)
                    : null,
                GasUsed = GetConsumedGas(result)
            };
            #endregion

            #region apply operation
            Db.TryAttach(sender);
            sender.LastLevel = delegation.Level;
            sender.Balance -= delegation.BakerFee;
            sender.Counter = delegation.Counter;
            sender.DelegationsCount++;

            if (prevDelegate != null)
            {
                Db.TryAttach(prevDelegate);
                prevDelegate.LastLevel = delegation.Level;
                prevDelegate.StakingBalance -= delegation.BakerFee;
                if (prevDelegate != sender)
                {
                    prevDelegate.DelegatedBalance -= delegation.BakerFee;
                    prevDelegate.DelegationsCount++;
                }
            }

            if (newDelegate != null)
            {
                Db.TryAttach(newDelegate);
                newDelegate.LastLevel = delegation.Level;
                if (newDelegate != sender && newDelegate != prevDelegate)
                    newDelegate.DelegationsCount++;
            }

            Context.Proposer.Balance += delegation.BakerFee;
            Context.Proposer.StakingBalance += delegation.BakerFee;

            Context.Block.Operations |= Operations.Delegations;
            Context.Block.Fees += delegation.BakerFee;

            Cache.AppState.Get().DelegationOpsCount++;
            #endregion

            #region apply result
            if (delegation.Status == OperationStatus.Applied)
            {
                if (sender is Data.Models.Delegate baker)
                {
                    #region reactivate baker
                    delegation.PrevDeactivationLevel = baker.DeactivationLevel;

                    baker.DeactivationLevel = GracePeriod.Init(Context.Block.Level, Context.Protocol);
                    baker.Staked = true;

                    foreach (var delegator in await Db.Accounts.Where(x => x.DelegateId == baker.Id).ToListAsync())
                    {
                        Cache.Accounts.Add(delegator);
                        delegator.LastLevel = delegation.Level;
                        delegator.Staked = true;
                    }
                    #endregion
                }
                else
                {
                    if (prevDelegate != null)
                    {
                        #region reset current delegation
                        if (result.TryGetProperty("balance_updates", out var updates))
                            await Unstake(delegation, [.. updates.EnumerateArray()]);

                        delegation.PrevDelegationLevel = sender.DelegationLevel;

                        Undelegate(sender, prevDelegate);
                        #endregion
                    }

                    if (sender == newDelegate)
                    {
                        #region register baker
                        sender = newDelegate = UpgradeUser((sender as User)!);

                        if (sender.OriginationsCount != 0)
                        {
                            var weirdOriginations = await Db.OriginationOps
                                .AsNoTracking()
                                .Where(x => x.DelegateId == sender.Id && x.Status == OperationStatus.Applied)
                                .ToListAsync();

                            foreach (var origination in weirdOriginations)
                            {
                                var weirdDelegator = await Cache.Accounts.GetAsync(origination.ContractId!.Value);
                                var hasDelegated = await Db.DelegationOps
                                    .AsNoTracking()
                                    .Where(x => x.SenderId == weirdDelegator.Id && x.Status == OperationStatus.Applied)
                                    .AnyAsync();

                                if (!hasDelegated)
                                {
                                    Db.TryAttach(weirdDelegator);
                                    weirdDelegator.LastLevel = delegation.Level;
                                    Delegate(weirdDelegator, (sender as Data.Models.Delegate)!, origination.Level);
                                }
                            }
                        }
                        #endregion
                    }
                    else if (newDelegate is Data.Models.Delegate _newDelegate)
                    {
                        Delegate(sender, _newDelegate, delegation.Level);
                    }
                }
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.DelegationOps.Add(delegation);
            Context.DelegationOps.Add(delegation);
        }

        public virtual async Task ApplyInternal(Block block, ManagerOperation parent, JsonElement content)
        {
            #region init
            var initiator = await Cache.Accounts.GetAsync(parent.SenderId);
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));
            var prevDelegate = sender.DelegateId is int senderDelegateId
                ? await Cache.Accounts.GetAsync(senderDelegateId) as Data.Models.Delegate
                : null;
            var newDelegate = content.OptionalString("delegate") is string _delegateAddress
                ? await Cache.Accounts.GetOrCreateAsync(_delegateAddress)
                : null;

            var result = content.Required("result");

            var delegation = new DelegationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = parent.Level,
                Timestamp = parent.Timestamp,
                OpHash = parent.OpHash,
                Counter = parent.Counter,
                Nonce = content.RequiredInt32("nonce"),
                InitiatorId = initiator.Id,
                SenderId = sender.Id,
                SenderCodeHash = (sender as Contract)?.CodeHash,
                DelegateId = newDelegate?.Id,
                PrevDelegateId = prevDelegate?.Id,
                Amount = sender.Balance,
                Status = result.RequiredString("status") switch
                {
                    "applied" => OperationStatus.Applied,
                    "backtracked" => OperationStatus.Backtracked,
                    "failed" => OperationStatus.Failed,
                    "skipped" => OperationStatus.Skipped,
                    _ => throw new NotImplementedException()
                },
                Errors = result.TryGetProperty("errors", out var errors)
                    ? OperationErrors.Parse(content, errors)
                    : null,
                GasUsed = GetConsumedGas(result)
            };
            #endregion

            #region apply operation
            if (parent is TransactionOperation parentTx)
            {
                parentTx.InternalOperations = (short?)((parentTx.InternalOperations ?? 0) + 1);
                parentTx.InternalDelegations = (short?)((parentTx.InternalDelegations ?? 0) + 1);
            }

            Db.TryAttach(sender);
            sender.LastLevel = delegation.Level;
            sender.DelegationsCount++;

            if (prevDelegate != null)
            {
                Db.TryAttach(prevDelegate);
                prevDelegate.LastLevel = delegation.Level;
                if (prevDelegate != sender)
                    prevDelegate.DelegationsCount++;
            }

            if (newDelegate != null)
            {
                Db.TryAttach(newDelegate);
                newDelegate.LastLevel = delegation.Level;
                if (newDelegate != sender && newDelegate != prevDelegate)
                    newDelegate.DelegationsCount++;
            }

            if (initiator != sender && initiator != prevDelegate && initiator != newDelegate)
            {
                initiator.DelegationsCount++;
            }

            Context.Block.Operations |= Operations.Delegations;

            Cache.AppState.Get().DelegationOpsCount++;
            #endregion

            #region apply result
            if (delegation.Status == OperationStatus.Applied)
            {
                if (prevDelegate != null)
                {
                    #region reset current delegation
                    //if (result.TryGetProperty("balance_updates", out var updates))
                    //    await Unstake(delegation, [.. updates.EnumerateArray()]);

                    delegation.PrevDelegationLevel = sender.DelegationLevel;

                    Undelegate(sender, prevDelegate);
                    #endregion
                }

                if (newDelegate is Data.Models.Delegate _newDelegate)
                {
                    Delegate(sender, _newDelegate, delegation.Level);
                }
            }
            #endregion

            Db.DelegationOps.Add(delegation);
            Context.DelegationOps.Add(delegation);
        }

        public virtual async Task Revert(Block block, DelegationOperation delegation)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(delegation.SenderId);
            var prevDelegate = delegation.PrevDelegateId is int prevDelegateId
                ? await Cache.Accounts.GetAsync(prevDelegateId) as Data.Models.Delegate
                : null;
            var newDelegate = delegation.DelegateId is int delegateId
                ? await Cache.Accounts.GetAsync(delegateId)
                : null;

            Db.TryAttach(sender);
            Db.TryAttach(prevDelegate);
            Db.TryAttach(newDelegate);
            Db.TryAttach(Context.Proposer);
            #endregion

            #region revert result
            if (delegation.Status == OperationStatus.Applied)
            {
                if (sender is Data.Models.Delegate baker)
                {
                    if (delegation.PrevDeactivationLevel is int prevDeactivationLevel)
                    {
                        #region deactivate baker
                        baker.DeactivationLevel = prevDeactivationLevel;
                        baker.Staked = false;

                        foreach (var delegator in await Db.Accounts.Where(x => x.DelegateId == baker.Id).ToListAsync())
                        {
                            Cache.Accounts.Add(delegator);
                            delegator.LastLevel = delegation.Level;
                            delegator.Staked = false;
                        }
                        #endregion
                    }
                    else
                    {
                        #region unregister baker
                        if (baker.DelegatorsCount != 0)
                        {
                            var weirdOriginations = await Db.OriginationOps
                                .AsNoTracking()
                                .Where(x => x.DelegateId == baker.Id && x.Status == OperationStatus.Applied)
                                .ToListAsync();

                            foreach (var origination in weirdOriginations)
                            {
                                var weirdDelegator = await Cache.Accounts.GetAsync(origination.ContractId!.Value);
                                var delegated = await Db.DelegationOps
                                    .AsNoTracking()
                                    .Where(x => x.SenderId == weirdDelegator.Id && x.Status == OperationStatus.Applied)
                                    .AnyAsync();

                                if (!delegated)
                                {
                                    Db.TryAttach(weirdDelegator);
                                    weirdDelegator.LastLevel = delegation.Level;
                                    Undelegate(weirdDelegator, baker);
                                }
                            }
                        }

                        sender = newDelegate = DowngradeDelegate(baker);

                        if (prevDelegate != null)
                        {
                            Delegate(sender, prevDelegate, delegation.PrevDelegationLevel!.Value);
                            await RevertUnstake(delegation);
                        }
                        #endregion
                    }
                }
                else
                {
                    if (newDelegate is Data.Models.Delegate _newDelegate)
                    {
                        Undelegate(sender, _newDelegate);
                    }

                    if (prevDelegate != null)
                    {
                        Delegate(sender, prevDelegate, delegation.PrevDelegationLevel!.Value);
                        await RevertUnstake(delegation);
                    }
                }
            }
            #endregion

            #region revert operation
            sender.LastLevel = delegation.Level;
            sender.Balance += delegation.BakerFee;
            sender.Counter = delegation.Counter - 1;
            if (sender is User user) user.Revealed = true;
            sender.DelegationsCount--;

            if (prevDelegate != null)
            {
                prevDelegate.LastLevel = delegation.Level;
                prevDelegate.StakingBalance += delegation.BakerFee;
                if (prevDelegate != sender)
                {
                    prevDelegate.DelegatedBalance += delegation.BakerFee;
                    prevDelegate.DelegationsCount--;
                }
            }

            if (newDelegate != null)
            {
                newDelegate.LastLevel = delegation.Level;
                if (newDelegate != sender && newDelegate != prevDelegate)
                    newDelegate.DelegationsCount--;
            }

            Context.Proposer.Balance -= delegation.BakerFee;
            Context.Proposer.StakingBalance -= delegation.BakerFee;

            Cache.AppState.Get().DelegationOpsCount--;
            #endregion

            Db.DelegationOps.Remove(delegation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        public virtual async Task RevertInternal(Block block, DelegationOperation delegation)
        {
            #region init
            var initiator = await Cache.Accounts.GetAsync(delegation.InitiatorId!.Value);
            var sender = await Cache.Accounts.GetAsync(delegation.SenderId);
            var prevDelegate = delegation.PrevDelegateId is int prevDelegateId
                ? await Cache.Accounts.GetAsync(prevDelegateId) as Data.Models.Delegate
                : null;
            var newDelegate = delegation.DelegateId is int delegateId
                ? await Cache.Accounts.GetAsync(delegateId)
                : null;

            Db.TryAttach(initiator);
            Db.TryAttach(sender);
            Db.TryAttach(prevDelegate);
            Db.TryAttach(newDelegate);
            #endregion

            #region revert result
            if (delegation.Status == OperationStatus.Applied)
            {
                if (newDelegate is Data.Models.Delegate _newDelegate)
                {
                    Undelegate(sender, _newDelegate);
                }

                if (prevDelegate != null)
                {
                    Delegate(sender, prevDelegate, delegation.PrevDelegationLevel!.Value);
                    //await RevertUnstake(delegation);
                }
            }
            #endregion

            #region revert operation
            sender.LastLevel = delegation.Level;
            sender.DelegationsCount--;

            if (prevDelegate != null)
            {
                prevDelegate.LastLevel = delegation.Level;
                if (prevDelegate != sender)
                    prevDelegate.DelegationsCount--;
            }

            if (newDelegate != null)
            {
                newDelegate.LastLevel = delegation.Level;
                if (newDelegate != sender && newDelegate != prevDelegate)
                    newDelegate.DelegationsCount--;
            }

            if (initiator != sender && initiator != prevDelegate && initiator != newDelegate)
            {
                initiator.DelegationsCount--;
            }

            Cache.AppState.Get().DelegationOpsCount--;
            #endregion

            Db.DelegationOps.Remove(delegation);
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual int GetConsumedGas(JsonElement result)
        {
            return result.OptionalInt32("consumed_gas") ?? 0;
        }

        Data.Models.Delegate UpgradeUser(User user)
        {
            var delegat = new Data.Models.Delegate
            {
                ActivationLevel = Context.Block.Level,
                Address = user.Address,
                FirstLevel = user.FirstLevel,
                LastLevel = user.LastLevel,
                Balance = user.Balance,
                Counter = user.Counter,
                DeactivationLevel = GracePeriod.Init(Context.Block.Level, Context.Protocol),
                DelegateId = null,
                DelegationLevel = null,
                Id = user.Id,
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
                StakedPseudotokens = user.StakedPseudotokens,
                UnstakedBalance = user.UnstakedBalance,
                UnstakedBakerId = user.UnstakedBakerId,
                StakingOpsCount = user.StakingOpsCount,
                DelegatedBalance = 0, 
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

            var isAdded = Db.Entry(user).State == EntityState.Added;
            Db.Entry(user).State = EntityState.Detached;
            Db.Entry(delegat).State = isAdded ? EntityState.Added : EntityState.Modified;
            Cache.Accounts.Add(delegat);

            return delegat;
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
                DelegateId = null,
                DelegationLevel = null,
                StakedPseudotokens = delegat.StakedPseudotokens,
                UnstakedBalance = delegat.UnstakedBalance,
                UnstakedBakerId = delegat.UnstakedBakerId,
                StakingOpsCount = delegat.StakingOpsCount,
                Id = delegat.Id,
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
                SetDelegateParametersOpsCount = delegat.SetDelegateParametersOpsCount,
                DalPublishCommitmentOpsCount = delegat.DalPublishCommitmentOpsCount,
                RefutationGamesCount = delegat.RefutationGamesCount,
                ActiveRefutationGamesCount = delegat.ActiveRefutationGamesCount,
                StakingUpdatesCount = delegat.StakingUpdatesCount
            };

            var isAdded = Db.Entry(delegat).State == EntityState.Added;
            Db.Entry(delegat).State = EntityState.Detached;
            Db.Entry(user).State = isAdded ? EntityState.Added : EntityState.Modified;
            Cache.Accounts.Add(user);

            return user;
        }

        void Delegate(Account delegator, Data.Models.Delegate baker, int delegationLevel)
        {
            delegator.DelegateId = baker.Id;
            delegator.DelegationLevel = delegationLevel;
            delegator.Staked = baker.Staked;

            baker.DelegatorsCount++;
            baker.StakingBalance += delegator.Balance - ((delegator as User)?.UnstakedBalance ?? 0);
            baker.DelegatedBalance += delegator.Balance - ((delegator as User)?.UnstakedBalance ?? 0);
        }

        void Undelegate(Account delegator, Data.Models.Delegate baker)
        {
            baker.DelegatorsCount--;
            baker.StakingBalance -= delegator.Balance - ((delegator as User)?.UnstakedBalance ?? 0);
            baker.DelegatedBalance -= delegator.Balance - ((delegator as User)?.UnstakedBalance ?? 0);

            delegator.DelegateId = null;
            delegator.DelegationLevel = null;
            delegator.Staked = false;
        }

        protected virtual Task Unstake(DelegationOperation op, List<JsonElement> balanceUpdates) => Task.CompletedTask;

        protected virtual Task RevertUnstake(DelegationOperation op) => Task.CompletedTask;
    }
}
