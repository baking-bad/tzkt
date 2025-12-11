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
            PayFee(sender, delegation.BakerFee);
            sender.LastLevel = delegation.Level;
            sender.Counter = delegation.Counter;
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

            Context.Block.Operations |= Operations.Delegations;

            Cache.AppState.Get().DelegationOpsCount++;
            #endregion

            #region apply result
            if (delegation.Status == OperationStatus.Applied)
            {
                if (sender is Data.Models.Delegate baker)
                {
                    #region reactivate baker
                    delegation.PrevDeactivationLevel = baker.DeactivationLevel;

                    await ActivateBaker(baker);
                    baker.DeactivationLevel = GracePeriod.Init(Context.Block.Level, Context.Protocol);
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
                        sender = newDelegate = RegisterBaker((sender as User)!);

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
            #endregion

            #region revert result
            if (delegation.Status == OperationStatus.Applied)
            {
                if (sender is Data.Models.Delegate baker)
                {
                    if (delegation.PrevDeactivationLevel is int prevDeactivationLevel)
                    {
                        #region deactivate baker
                        await DeactivateBaker(baker);
                        baker.DeactivationLevel = prevDeactivationLevel;
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

                        sender = newDelegate = UnregisterBaker(baker);

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
            RevertPayFee(sender, delegation.BakerFee);
            sender.LastLevel = delegation.Level;
            sender.Counter = delegation.Counter - 1;
            if (sender is User user) user.Revealed = true;
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

        protected virtual Task Unstake(DelegationOperation op, List<JsonElement> balanceUpdates) => Task.CompletedTask;

        protected virtual Task RevertUnstake(DelegationOperation op) => Task.CompletedTask;
    }
}
