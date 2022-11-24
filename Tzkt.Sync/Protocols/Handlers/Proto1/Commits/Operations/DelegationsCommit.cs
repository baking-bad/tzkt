using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);
            var prevDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            var newDelegate = Cache.Accounts.GetDelegateOrDefault(content.OptionalString("delegate"));
            var result = content.Required("metadata").Required("operation_result");

            var delegation = new DelegationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                Sender = sender,
                Delegate = newDelegate,
                PrevDelegate = prevDelegate,
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
            //var block = delegation.Block;
            var blockBaker = block.Proposer;

            //Db.TryAttach(block);
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
            #endregion

            #region apply result
            if (delegation.Status == OperationStatus.Applied)
            {
                if (content.RequiredString("source") == content.OptionalString("delegate"))
                {
                    if (sender.Type == AccountType.User)
                    {
                        ResetDelegate(sender, prevDelegate);
                        UpgradeUser(delegation);

                        #region weird delegators
                        var delegat = (Data.Models.Delegate)delegation.Sender;

                        var weirdOriginations = await Db.OriginationOps
                            .Include(x => x.Contract)
                            .Where(x => x.Contract != null && x.Contract.WeirdDelegateId == delegat.Id)
                            .ToListAsync();

                        foreach (var origination in weirdOriginations)
                        {
                            Db.TryAttach(origination);
                            origination.Delegate = delegat;
                            if (delegat.Id != origination.SenderId && delegat.Id != origination.ManagerId) delegat.OriginationsCount++;

                            if (origination.Contract.DelegationsCount == 0)
                            {
                                Db.TryAttach(origination.Contract);
                                Cache.Accounts.Add(origination.Contract);

                                SetDelegate(origination.Contract, delegat, origination.Level);
                            }
                        }
                        #endregion
                    }
                    else if (sender is Data.Models.Delegate delegat)
                    {
                        delegation.ResetDeactivation = delegat.DeactivationLevel;
                        await ReactivateDelegate(delegation);
                    }
                }
                else
                {
                    ResetDelegate(sender, prevDelegate);
                    if (newDelegate != null)
                        SetDelegate(sender, newDelegate, block.Level);
                }
            }
            #endregion

            Proto.Manager.Set(delegation.Sender);
            Db.DelegationOps.Add(delegation);
        }

        public virtual async Task ApplyInternal(Block block, TransactionOperation parent, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"))
                ?? block.Originations?.FirstOrDefault(x => x.Contract.Address == content.RequiredString("source"))?.Contract
                    ?? throw new ValidationException("Delegation source address doesn't exist");

            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);
            var prevDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            var newDelegate = Cache.Accounts.GetDelegateOrDefault(content.OptionalString("delegate"));
            var result = content.Required("result");

            var delegation = new DelegationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Initiator = parent.Sender,
                Block = parent.Block,
                Level = parent.Block.Level,
                Timestamp = parent.Timestamp,
                OpHash = parent.OpHash,
                Counter = parent.Counter,
                Nonce = content.RequiredInt32("nonce"),
                Sender = sender,
                SenderCodeHash = (sender as Contract)?.CodeHash,
                Delegate = newDelegate,
                PrevDelegate = prevDelegate,
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
            var parentTx = parent;
            var parentSender = parentTx.Sender;

            Db.TryAttach(sender);
            Db.TryAttach(parentSender);
            Db.TryAttach(prevDelegate);
            Db.TryAttach(newDelegate);
            #endregion

            #region apply operation
            parentTx.InternalOperations = (short?)((parentTx.InternalOperations ?? 0) + 1);
            parentTx.InternalDelegations = (short?)((parentTx.InternalDelegations ?? 0) + 1);

            sender.DelegationsCount++;
            if (prevDelegate != null && prevDelegate != sender) prevDelegate.DelegationsCount++;
            if (newDelegate != null && newDelegate != sender && newDelegate != prevDelegate) newDelegate.DelegationsCount++;
            if (parentSender != sender && parentSender != prevDelegate && parentSender != newDelegate) parentSender.DelegationsCount++;

            block.Operations |= Operations.Delegations;
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
        }

        public virtual async Task Revert(Block block, DelegationOperation delegation)
        {
            #region init
            delegation.Block ??= block;
            delegation.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            delegation.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            delegation.Sender ??= await Cache.Accounts.GetAsync(delegation.SenderId);
            delegation.Sender.Delegate ??= Cache.Accounts.GetDelegate(delegation.Sender.DelegateId);
            delegation.Delegate ??= Cache.Accounts.GetDelegate(delegation.DelegateId);
            delegation.PrevDelegate ??= Cache.Accounts.GetDelegate(delegation.PrevDelegateId);
            #endregion

            #region entities
            //var block = delegation.Block;
            var blockBaker = block.Proposer;
            var sender = delegation.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            var newDelegate = delegation.Delegate;
            var prevDelegation = await GetPrevDelegationAsync(sender, delegation.Id);
            var prevDelegationLevel = prevDelegation?.Level;
            var prevDelegate = prevDelegation?.Delegate;

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
                        prevDelegate = origination.Delegate;
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
                delegation.Sender = sender;
                delegation.Delegate = newDelegate;

                if (sender.Id == newDelegate?.Id)
                {
                    if (delegation.ResetDeactivation == null)
                    {
                        #region weird delegations
                        var delegat = (Data.Models.Delegate)delegation.Sender;

                        var weirdOriginations = await Db.OriginationOps
                            .Include(x => x.Contract)
                            .Where(x => x.Contract != null && x.Contract.WeirdDelegateId == delegat.Id)
                            .ToListAsync();

                        foreach (var origination in weirdOriginations)
                        {
                            Db.TryAttach(origination);
                            origination.Delegate = null;
                            if (delegat.Id != origination.SenderId && delegat.Id != origination.ManagerId) delegat.OriginationsCount--;

                            if (origination.Contract.DelegationsCount == 0)
                            {
                                Db.TryAttach(origination.Contract);
                                Cache.Accounts.Add(origination.Contract);

                                ResetDelegate(origination.Contract, delegat);
                            }
                        }
                        #endregion

                        ResetDelegate(sender, senderDelegate);
                        DowngradeDelegate(delegation);

                        sender = delegation.Sender;

                        if (prevDelegate != null && prevDelegate.Id != sender.Id)
                            SetDelegate(sender, prevDelegate, (int)prevDelegationLevel);
                    }
                    else
                    {
                        await DeactivateDelegate(delegation, (int)delegation.ResetDeactivation);
                    }
                }
                else
                {
                    ResetDelegate(sender, senderDelegate);
                    if (prevDelegate != null && prevDelegate.Id != sender.Id)
                        SetDelegate(sender, prevDelegate, (int)prevDelegationLevel);
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
            (sender as User).Revealed = true;
            #endregion

            Db.DelegationOps.Remove(delegation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        public virtual async Task RevertInternal(Block block, DelegationOperation delegation)
        {
            #region init
            delegation.Block ??= block;
            delegation.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            delegation.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            delegation.Sender ??= await Cache.Accounts.GetAsync(delegation.SenderId);
            delegation.Sender.Delegate ??= Cache.Accounts.GetDelegate(delegation.Sender.DelegateId);
            delegation.Delegate ??= Cache.Accounts.GetDelegate(delegation.DelegateId);
            delegation.PrevDelegate ??= Cache.Accounts.GetDelegate(delegation.PrevDelegateId);

            delegation.Initiator = await Cache.Accounts.GetAsync(delegation.InitiatorId);
            delegation.Initiator.Delegate ??= Cache.Accounts.GetDelegate(delegation.Initiator.DelegateId);
            #endregion

            #region entities
            var parentSender = delegation.Initiator;
            var sender = delegation.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var newDelegate = delegation.Delegate;

            var prevDelegation = await GetPrevDelegationAsync(sender, delegation.Id);
            var prevDelegationLevel = prevDelegation?.Level;
            var prevDelegate = prevDelegation?.Delegate;

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
                    prevDelegate = origination.Delegate;
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
            #endregion

            Db.DelegationOps.Remove(delegation);
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual int GetConsumedGas(JsonElement result)
        {
            return result.OptionalInt32("consumed_gas") ?? 0;
        }

        void UpgradeUser(DelegationOperation delegation)
        {
            var user = delegation.Sender as User;

            var delegat = new Data.Models.Delegate
            {
                ActivationLevel = delegation.Level,
                Address = user.Address,
                FirstLevel = user.FirstLevel,
                LastLevel = user.LastLevel,
                Balance = user.Balance,
                Counter = user.Counter,
                DeactivationLevel = GracePeriod.Init(delegation.Block),
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
                StakingBalance = user.Balance,
                DelegatedBalance = 0,
                Type = AccountType.Delegate,
                ActiveTokensCount = user.ActiveTokensCount,
                TokenBalancesCount = user.TokenBalancesCount,
                TokenTransfersCount = user.TokenTransfersCount,
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
                RollupsCount = user.RollupsCount
            };

            #region update relations
            var touched = new List<(object entry, EntityState state)>();
            foreach (var entry in Db.ChangeTracker.Entries())
            {
                switch (entry.Entity)
                {
                    case ActivationOperation op:
                        if (op.Account?.Id == user.Id)
                        {
                            op.Account = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case DelegationOperation op:
                        if (op.Sender?.Id == user.Id || op.Initiator?.Id == user.Id)
                        {
                            if (op.Sender?.Id == user.Id)
                                op.Sender = delegat;

                            if (op.Initiator?.Id == user.Id)
                                op.Initiator = delegat;

                            touched.Add((op, entry.State));
                        }
                        break;
                    case OriginationOperation op:
                        if (op.Sender?.Id == user.Id || op.Manager?.Id == user.Id || op.Initiator?.Id == user.Id)
                        {
                            if (op.Sender?.Id == user.Id)
                                op.Sender = delegat;

                            if (op.Initiator?.Id == user.Id)
                                op.Initiator = delegat;

                            if (op.Manager?.Id == user.Id)
                                op.Manager = delegat;

                            touched.Add((op, entry.State));
                        }
                        break;
                    case RegisterConstantOperation op:
                        if (op.Sender?.Id == user.Id)
                        {
                            op.Sender = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case RevealOperation op:
                        if (op.Sender?.Id == user.Id)
                        {
                            op.Sender = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case SetDepositsLimitOperation op:
                        if (op.Sender?.Id == user.Id)
                        {
                            op.Sender = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TransactionOperation op:
                        if (op.Sender?.Id == user.Id || op.Target?.Id == user.Id || op.Initiator?.Id == user.Id)
                        {
                            if (op.Sender?.Id == user.Id)
                                op.Sender = delegat;

                            if (op.Initiator?.Id == user.Id)
                                op.Initiator = delegat;

                            if (op.Target?.Id == user.Id)
                                op.Target = delegat;

                            touched.Add((op, entry.State));
                        }
                        break;
                    case TransferTicketOperation op:
                        if (op.Sender?.Id == user.Id)
                        {
                            op.Sender = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupCommitOperation op:
                        if (op.Sender?.Id == user.Id)
                        {
                            op.Sender = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupDispatchTicketsOperation op:
                        if (op.Sender?.Id == user.Id)
                        {
                            op.Sender = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupFinalizeCommitmentOperation op:
                        if (op.Sender?.Id == user.Id)
                        {
                            op.Sender = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupOriginationOperation op:
                        if (op.Sender?.Id == user.Id)
                        {
                            op.Sender = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupRejectionOperation op:
                        if (op.Sender?.Id == user.Id)
                        {
                            op.Sender = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupRemoveCommitmentOperation op:
                        if (op.Sender?.Id == user.Id)
                        {
                            op.Sender = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupReturnBondOperation op:
                        if (op.Sender?.Id == user.Id)
                        {
                            op.Sender = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupSubmitBatchOperation op:
                        if (op.Sender?.Id == user.Id)
                        {
                            op.Sender = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case IncreasePaidStorageOperation op:
                        if (op.Sender?.Id == user.Id)
                        {
                            op.Sender = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case UpdateConsensusKeyOperation op:
                        if (op.Sender?.Id == user.Id)
                        {
                            op.Sender = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case Contract contract:
                        if (contract.WeirdDelegate?.Id == user.Id || contract.Creator?.Id == user.Id || contract.Manager?.Id == user.Id)
                        {
                            if (contract.WeirdDelegate?.Id == user.Id)
                                contract.WeirdDelegate = delegat;

                            if (contract.Creator?.Id == user.Id)
                                contract.Creator = delegat;

                            if (contract.Manager?.Id == user.Id)
                                contract.Manager = delegat;

                            touched.Add((contract, entry.State));
                        }
                        break;
                }
            }
            #endregion

            var isAdded = Db.Entry(user).State == EntityState.Added;
            Db.Entry(user).State = EntityState.Detached;
            Db.Entry(delegat).State = isAdded ? EntityState.Added : EntityState.Modified;
            Cache.Accounts.Add(delegat);

            #region update graph
            foreach (var (entry, state) in touched)
                Db.Entry(entry).State = state;
            #endregion

            delegation.Sender = delegation.Delegate = delegat;
        }

        void DowngradeDelegate(DelegationOperation delegation)
        {
            var delegat = delegation.Delegate;

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
                RollupsCount = delegat.RollupsCount
            };

            #region update relations
            var touched = new List<(object entry, EntityState state)>();
            foreach (var entry in Db.ChangeTracker.Entries())
            {
                switch (entry.Entity)
                {
                    case ActivationOperation op:
                        if (op.Account?.Id == delegat.Id)
                        {
                            op.Account = user;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case DelegationOperation op:
                        if (op.Sender?.Id == delegat.Id || op.Delegate?.Id == delegat.Id || op.Initiator?.Id == delegat.Id || op.PrevDelegate?.Id == delegat.Id)
                        {
                            if (op.Sender?.Id == delegat.Id)
                                op.Sender = user;

                            if (op.Initiator?.Id == delegat.Id)
                                op.Initiator = user;

                            if (op.Delegate?.Id == delegat.Id)
                                op.Delegate = null;

                            if (op.PrevDelegate?.Id == delegat.Id)
                                op.PrevDelegate = null;

                            touched.Add((op, entry.State));
                        }
                        break;
                    case OriginationOperation op:
                        if (op.Sender?.Id == delegat.Id || op.Manager?.Id == delegat.Id || op.Delegate?.Id == delegat.Id || op.Initiator?.Id == delegat.Id)
                        {
                            if (op.Sender?.Id == delegat.Id)
                                op.Sender = user;

                            if (op.Initiator?.Id == delegat.Id)
                                op.Initiator = user;

                            if (op.Manager?.Id == delegat.Id)
                                op.Manager = user;

                            if (op.Delegate?.Id == delegat.Id)
                                op.Delegate = null;

                            touched.Add((op, entry.State));
                        }
                        break;
                    case RegisterConstantOperation op:
                        if (op.Sender?.Id == delegat.Id)
                        {
                            op.Sender = user;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case RevealOperation op:
                        if (op.Sender?.Id == delegat.Id)
                        {
                            op.Sender = user;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case SetDepositsLimitOperation op:
                        if (op.Sender?.Id == delegat.Id)
                        {
                            op.Sender = user;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TransactionOperation op:
                        if (op.Sender?.Id == delegat.Id || op.Target?.Id == delegat.Id || op.Initiator?.Id == delegat.Id)
                        {
                            if (op.Sender?.Id == delegat.Id)
                                op.Sender = user;

                            if (op.Initiator?.Id == delegat.Id)
                                op.Initiator = user;

                            if (op.Target?.Id == delegat.Id)
                                op.Target = user;

                            touched.Add((op, entry.State));
                        }
                        break;
                    case TransferTicketOperation op:
                        if (op.Sender?.Id == delegat.Id)
                        {
                            op.Sender = user;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupCommitOperation op:
                        if (op.Sender?.Id == delegat.Id)
                        {
                            op.Sender = user;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupDispatchTicketsOperation op:
                        if (op.Sender?.Id == delegat.Id)
                        {
                            op.Sender = user;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupFinalizeCommitmentOperation op:
                        if (op.Sender?.Id == delegat.Id)
                        {
                            op.Sender = user;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupOriginationOperation op:
                        if (op.Sender?.Id == delegat.Id)
                        {
                            op.Sender = user;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupRejectionOperation op:
                        if (op.Sender?.Id == delegat.Id)
                        {
                            op.Sender = user;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupRemoveCommitmentOperation op:
                        if (op.Sender?.Id == delegat.Id)
                        {
                            op.Sender = user;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupReturnBondOperation op:
                        if (op.Sender?.Id == delegat.Id)
                        {
                            op.Sender = user;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case TxRollupSubmitBatchOperation op:
                        if (op.Sender?.Id == delegat.Id)
                        {
                            op.Sender = user;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case IncreasePaidStorageOperation op:
                        if (op.Sender?.Id == delegat.Id)
                        {
                            op.Sender = user;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case UpdateConsensusKeyOperation op:
                        if (op.Sender?.Id == delegat.Id)
                        {
                            op.Sender = user;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case Contract contract:
                        if (contract.WeirdDelegate?.Id == delegat.Id || contract.Creator?.Id == delegat.Id || contract.Manager?.Id == delegat.Id)
                        {
                            if (contract.WeirdDelegate?.Id == delegat.Id)
                                contract.WeirdDelegate = user;

                            if (contract.Creator?.Id == delegat.Id)
                                contract.Creator = user;

                            if (contract.Manager?.Id == delegat.Id)
                                contract.Manager = user;

                            touched.Add((contract, entry.State));
                        }
                        break;
                }
            }
            #endregion

            var isAdded = Db.Entry(delegat).State == EntityState.Added;
            Db.Entry(delegat).State = EntityState.Detached;
            Db.Entry(user).State = isAdded ? EntityState.Added : EntityState.Modified;
            Cache.Accounts.Add(user);

            #region update graph
            foreach (var (entry, state) in touched)
                Db.Entry(entry).State = state;
            #endregion

            delegation.Sender = user;
            delegation.Delegate = null;
        }

        async Task ReactivateDelegate(DelegationOperation delegation)
        {
            var delegat = delegation.Sender as Data.Models.Delegate;

            delegat.DeactivationLevel = GracePeriod.Init(delegation.Block);
            delegat.Staked = true;

            foreach (var delegator in await Db.Accounts.Where(x => x.DelegateId == delegat.Id).ToListAsync())
            {
                Cache.Accounts.Add(delegator);
                delegator.Staked = true;
            }

            delegation.Delegate = delegat;
        }

        async Task DeactivateDelegate(DelegationOperation delegation, int deactivationLevel)
        {
            var delegat = delegation.Sender as Data.Models.Delegate;

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
            sender.Delegate = newDelegate;
            sender.DelegateId = newDelegate.Id;
            sender.DelegationLevel = level;
            sender.Staked = newDelegate.Staked;

            newDelegate.DelegatorsCount++;
            newDelegate.StakingBalance += sender.Balance;
            newDelegate.DelegatedBalance += sender.Balance;
        }

        void ResetDelegate(Account sender, Data.Models.Delegate prevDelegate)
        {
            if (prevDelegate != null)
            {
                if (sender.Address != prevDelegate.Address)
                {
                    prevDelegate.DelegatorsCount--;
                    prevDelegate.DelegatedBalance -= sender.Balance;
                }
                
                prevDelegate.StakingBalance -= sender.Balance;
            }

            sender.Delegate = null;
            sender.DelegateId = null;
            sender.DelegationLevel = null;
            sender.Staked = false;
        }

        async Task<OriginationOperation> GetOriginationAsync(Contract contract)
        {
            var result = await Db.OriginationOps
                .FirstAsync(x => x.Status == OperationStatus.Applied && x.ContractId == contract.Id);

            result.Delegate ??= Cache.Accounts.GetDelegate(result.DelegateId);

            return result;
        }

        async Task<DelegationOperation> GetPrevDelegationAsync(Account sender, long id)
        {
            var result = await Db.DelegationOps
                .Where(x => x.Status == OperationStatus.Applied &&
                    x.SenderId == sender.Id &&
                    x.Id < id)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (result != null)
            {
                result.Sender = sender;
                result.Delegate = Cache.Accounts.GetDelegate(result.DelegateId);
            }

            return result;
        }
    }
}
