using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto1
{
    class DelegationsCommit : ProtocolCommit
    {
        public DelegationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            var prevDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;
            var newDelegate = Cache.Accounts.GetDelegateOrDefault(content.OptionalString("delegate"));
            var result = content.Required("metadata").Required("operation_result");

            var delegation = new DelegationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
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

            #region entities
            var blockBaker = Context.Proposer;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(prevDelegate);
            Db.TryAttach(newDelegate);
            #endregion

            #region apply operation
            sender.Balance -= delegation.BakerFee;
            if (prevDelegate != null)
            {
                prevDelegate.StakingBalance -= delegation.BakerFee;
                if (prevDelegate.Id != sender.Id)
                    prevDelegate.DelegatedBalance -= delegation.BakerFee;
            }
            blockBaker.Balance += delegation.BakerFee;
            blockBaker.StakingBalance += delegation.BakerFee;

            sender.DelegationsCount++;
            if (prevDelegate != null && prevDelegate != sender) prevDelegate.DelegationsCount++;
            if (newDelegate != null && newDelegate != sender && newDelegate != prevDelegate) newDelegate.DelegationsCount++;

            block.Operations |= Operations.Delegations;
            block.Fees += delegation.BakerFee;

            sender.Counter = delegation.Counter;

            Cache.AppState.Get().DelegationOpsCount++;
            #endregion

            #region apply result
            if (delegation.Status == OperationStatus.Applied)
            {
                if (content.RequiredString("source") == content.OptionalString("delegate"))
                {
                    if (sender.Type == AccountType.User)
                    {
                        if (result.TryGetProperty("balance_updates", out var updates))
                            await Unstake(delegation, updates.EnumerateArray().ToList());
                        ResetDelegate(sender, prevDelegate);
                        sender = UpgradeUser(sender as User);
                        delegation.DelegateId = sender.Id;

                        #region weird delegators
                        var weirds = await Db.Contracts
                            .Join(Db.OriginationOps, x => x.Id, x => x.ContractId, (contract, origination) => new { contract, origination })
                            .Where(x => x.contract.WeirdDelegateId != null && x.contract.WeirdDelegateId == sender.Id)
                            .ToListAsync();

                        foreach (var weird in weirds)
                        {
                            Db.TryAttach(weird.origination);
                            weird.origination.DelegateId = sender.Id;
                            if (sender.Id != weird.origination.SenderId && sender.Id != weird.origination.ManagerId)
                                sender.OriginationsCount++;

                            if (weird.contract.DelegationsCount == 0)
                            {
                                Db.TryAttach(weird.contract);
                                Cache.Accounts.Add(weird.contract);

                                SetDelegate(weird.contract, sender as Data.Models.Delegate, weird.origination.Level);
                            }
                        }
                        #endregion
                    }
                    else if (sender is Data.Models.Delegate delegat)
                    {
                        delegation.ResetDeactivation = delegat.DeactivationLevel;
                        await ReactivateDelegate(delegat);
                    }
                }
                else
                {
                    if (result.TryGetProperty("balance_updates", out var updates))
                        await Unstake(delegation, updates.EnumerateArray().ToList());
                    ResetDelegate(sender, prevDelegate);
                    if (newDelegate != null)
                        SetDelegate(sender, newDelegate, block.Level);
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
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"))
                ?? throw new ValidationException("Delegation source address doesn't exist");

            var prevDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;
            var newDelegate = Cache.Accounts.GetDelegateOrDefault(content.OptionalString("delegate"));
            var result = content.Required("result");

            var delegation = new DelegationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                InitiatorId = parent.SenderId,
                Level = parent.Level,
                Timestamp = parent.Timestamp,
                OpHash = parent.OpHash,
                Counter = parent.Counter,
                Nonce = content.RequiredInt32("nonce"),
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

            #region entities
            var parentSender = await Cache.Accounts.GetAsync(parent.SenderId);

            Db.TryAttach(sender);
            Db.TryAttach(parentSender);
            Db.TryAttach(prevDelegate);
            Db.TryAttach(newDelegate);
            #endregion

            #region apply operation
            if (parent is TransactionOperation parentTx)
            {
                parentTx.InternalOperations = (short?)((parentTx.InternalOperations ?? 0) + 1);
                parentTx.InternalDelegations = (short?)((parentTx.InternalDelegations ?? 0) + 1);
            }

            sender.DelegationsCount++;
            if (prevDelegate != null && prevDelegate != sender) prevDelegate.DelegationsCount++;
            if (newDelegate != null && newDelegate != sender && newDelegate != prevDelegate) newDelegate.DelegationsCount++;
            if (parentSender != sender && parentSender != prevDelegate && parentSender != newDelegate) parentSender.DelegationsCount++;

            block.Operations |= Operations.Delegations;

            Cache.AppState.Get().DelegationOpsCount++;
            #endregion

            #region apply result
            if (delegation.Status == OperationStatus.Applied)
            {
                ResetDelegate(sender, prevDelegate);
                if (newDelegate != null)
                    SetDelegate(sender, newDelegate, block.Level);
            }
            #endregion

            Db.DelegationOps.Add(delegation);
            Context.DelegationOps.Add(delegation);
        }

        public virtual async Task Revert(Block block, DelegationOperation delegation)
        {
            #region entities
            var blockBaker = Context.Proposer;
            var sender = await Cache.Accounts.GetAsync(delegation.SenderId);
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;
            var newDelegate = Cache.Accounts.GetDelegate(delegation.DelegateId);

            var prevDelegation = await GetPrevDelegationAsync(sender, delegation.Id);
            var prevDelegationLevel = prevDelegation?.Level;
            var prevDelegate = Cache.Accounts.GetDelegate(prevDelegation?.DelegateId);

            if (prevDelegation == null)
            {
                if (sender is Contract contract)
                {
                    if (contract.WeirdDelegateId != null)
                    {
                        prevDelegate = Cache.Accounts.GetDelegateOrDefault(contract.WeirdDelegateId);
                        prevDelegationLevel = prevDelegate?.ActivationLevel;
                    }
                    else
                    {
                        var origination = await GetOriginationAsync(contract);
                        prevDelegate = Cache.Accounts.GetDelegate(origination.DelegateId);
                        prevDelegationLevel = origination.Level;
                    }
                }
                else if (sender is Data.Models.Delegate delegat)
                {
                    if (delegat.ActivationLevel < block.Level)
                    {
                        prevDelegate = delegat;
                    }
                }
            }

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(newDelegate);
            Db.TryAttach(prevDelegate);
            #endregion

            #region revert result
            if (delegation.Status == OperationStatus.Applied)
            {
                if (sender.Id == newDelegate?.Id)
                {
                    if (delegation.ResetDeactivation == null)
                    {
                        #region weird delegations
                        var weirds = await Db.Contracts
                            .Join(Db.OriginationOps, x => x.Id, x => x.ContractId, (contract, origination) => new { contract, origination })
                            .Where(x => x.contract.WeirdDelegateId != null && x.contract.WeirdDelegateId == sender.Id)
                            .ToListAsync();

                        foreach (var weird in weirds)
                        {
                            Db.TryAttach(weird.origination);
                            weird.origination.DelegateId = null;
                            if (sender.Id != weird.origination.SenderId && sender.Id != weird.origination.ManagerId)
                                sender.OriginationsCount--;

                            if (weird.contract.DelegationsCount == 0)
                            {
                                Db.TryAttach(weird.contract);
                                Cache.Accounts.Add(weird.contract);

                                ResetDelegate(weird.contract, sender as Data.Models.Delegate);
                            }
                        }
                        #endregion

                        ResetDelegate(sender, senderDelegate);
                        sender = DowngradeDelegate(sender as Data.Models.Delegate);
                        newDelegate = null;

                        if (prevDelegate != null && prevDelegate.Id != sender.Id)
                        {
                            SetDelegate(sender, prevDelegate, (int)prevDelegationLevel);
                            await RevertUnstake(delegation);
                        }
                    }
                    else
                    {
                        await DeactivateDelegate(sender as Data.Models.Delegate, (int)delegation.ResetDeactivation);
                    }
                }
                else
                {
                    ResetDelegate(sender, senderDelegate);
                    if (prevDelegate != null && prevDelegate.Id != sender.Id)
                    {
                        SetDelegate(sender, prevDelegate, (int)prevDelegationLevel);
                        await RevertUnstake(delegation);
                    }
                }
            }
            #endregion

            #region revert operation
            sender.Balance += delegation.BakerFee;
            if (prevDelegate != null)
            {
                prevDelegate.StakingBalance += delegation.BakerFee;
                if (prevDelegate.Id != sender.Id)
                    prevDelegate.DelegatedBalance += delegation.BakerFee;
            }
            blockBaker.Balance -= delegation.BakerFee;
            blockBaker.StakingBalance -= delegation.BakerFee;

            sender.DelegationsCount--;
            if (prevDelegate != null && prevDelegate != sender) prevDelegate.DelegationsCount--;
            if (newDelegate != null && newDelegate != sender && newDelegate != prevDelegate) newDelegate.DelegationsCount--;

            sender.Counter = delegation.Counter - 1;
            if (sender is User user) user.Revealed = true;

            Cache.AppState.Get().DelegationOpsCount--;
            #endregion

            Db.DelegationOps.Remove(delegation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        public virtual async Task RevertInternal(Block block, DelegationOperation delegation)
        {
            #region entities
            var parentSender = await Cache.Accounts.GetAsync(delegation.InitiatorId);
            var sender = await Cache.Accounts.GetAsync(delegation.SenderId);
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;

            var newDelegate = Cache.Accounts.GetDelegate(delegation.DelegateId);

            var prevDelegation = await GetPrevDelegationAsync(sender, delegation.Id);
            var prevDelegationLevel = prevDelegation?.Level;
            var prevDelegate = Cache.Accounts.GetDelegate(prevDelegation?.DelegateId);

            if (prevDelegation == null && sender is Contract contract)
            {
                if (contract.WeirdDelegateId != null)
                {
                    prevDelegate = await Cache.Accounts.GetAsync(contract.WeirdDelegateId) as Data.Models.Delegate;
                    prevDelegationLevel = prevDelegate?.ActivationLevel;
                }
                else
                {
                    var origination = await GetOriginationAsync(contract);
                    prevDelegate = Cache.Accounts.GetDelegate(origination.DelegateId);
                    prevDelegationLevel = origination.Level;
                }
            }

            //Db.TryAttach(block);

            Db.TryAttach(parentSender);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(newDelegate);
            Db.TryAttach(prevDelegate);
            #endregion

            #region revert result
            if (delegation.Status == OperationStatus.Applied)
            {
                ResetDelegate(sender, senderDelegate);
                if (prevDelegate != null)
                    SetDelegate(sender, prevDelegate, (int)prevDelegationLevel);
            }
            #endregion

            #region revert operation
            sender.DelegationsCount--;
            if (prevDelegate != null && prevDelegate != sender) prevDelegate.DelegationsCount--;
            if (newDelegate != null && newDelegate != sender && newDelegate != prevDelegate) newDelegate.DelegationsCount--;
            if (parentSender != sender && parentSender != prevDelegate && parentSender != newDelegate) parentSender.DelegationsCount--;

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

        async Task ReactivateDelegate(Data.Models.Delegate delegat)
        {
            delegat.DeactivationLevel = GracePeriod.Init(Context.Block.Level, Context.Protocol);
            delegat.Staked = true;

            foreach (var delegator in await Db.Accounts.Where(x => x.DelegateId == delegat.Id).ToListAsync())
            {
                Cache.Accounts.Add(delegator);
                delegator.Staked = true;
            }
        }

        async Task DeactivateDelegate(Data.Models.Delegate delegat, int deactivationLevel)
        {
            delegat.DeactivationLevel = deactivationLevel;
            delegat.Staked = false;

            foreach (var delegator in await Db.Accounts.Where(x => x.DelegateId == delegat.Id).ToListAsync())
            {
                Cache.Accounts.Add(delegator);
                delegator.Staked = false;
            }
        }

        void SetDelegate(Account sender, Data.Models.Delegate newDelegate, int level)
        {
            sender.DelegateId = newDelegate.Id;
            sender.DelegationLevel = level;
            sender.Staked = newDelegate.Staked;

            newDelegate.DelegatorsCount++;
            newDelegate.StakingBalance += sender.Balance - ((sender as User)?.UnstakedBalance ?? 0);
            newDelegate.DelegatedBalance += sender.Balance - ((sender as User)?.UnstakedBalance ?? 0);
        }

        void ResetDelegate(Account sender, Data.Models.Delegate prevDelegate)
        {
            if (prevDelegate != null)
            {
                if (sender.Address != prevDelegate.Address)
                {
                    prevDelegate.DelegatorsCount--;
                    prevDelegate.DelegatedBalance -= sender.Balance - ((sender as User)?.UnstakedBalance ?? 0);
                }

                prevDelegate.StakingBalance -= sender.Balance - ((sender as User)?.UnstakedBalance ?? 0);
            }

            sender.DelegateId = null;
            sender.DelegationLevel = null;
            sender.Staked = false;
        }

        protected virtual Task Unstake(DelegationOperation op, List<JsonElement> balanceUpdates) => Task.CompletedTask;

        protected virtual Task RevertUnstake(DelegationOperation op) => Task.CompletedTask;

        Task<OriginationOperation> GetOriginationAsync(Contract contract)
        {
            return Db.OriginationOps
                .FirstAsync(x => x.Status == OperationStatus.Applied && x.ContractId == contract.Id);
        }

        Task<DelegationOperation> GetPrevDelegationAsync(Account sender, long id)
        {
            return Db.DelegationOps
                .Where(x =>
                    x.Status == OperationStatus.Applied &&
                    x.SenderId == sender.Id &&
                    x.Id < id)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
        }
    }
}
