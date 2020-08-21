using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto6
{
    class DelegationsCommit : ProtocolCommit
    {
        public TransactionOperation Parent { get; private set; }
        public DelegationOperation Delegation { get; private set; }
        public bool IsSelfDelegation { get; private set; }

        DelegationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawDelegationContent content)
        {
            var sender = await Cache.Accounts.GetAsync(content.Source);
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);

            var delegat = Cache.Accounts.GetDelegateOrDefault(content.Delegate);

            IsSelfDelegation = content.Source == content.Delegate;
            Delegation = new DelegationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.Hash,
                BakerFee = content.Fee,
                Counter = content.Counter,
                GasLimit = content.GasLimit,
                StorageLimit = content.StorageLimit,
                Sender = sender,
                Delegate = delegat,
                PrevDelegate = sender.Delegate,
                Amount = sender.Balance - content.Fee,
                Status = content.Metadata.Result.Status switch
                {
                    "applied" => OperationStatus.Applied,
                    "backtracked" => OperationStatus.Backtracked,
                    "failed" => OperationStatus.Failed,
                    "skipped" => OperationStatus.Skipped,
                    _ => throw new Exception($"Invalid status '{content.Metadata.Result.Status}'")
                },
                Errors = OperationErrors.Parse(content.Metadata.Result.Errors),
                GasUsed = content.Metadata.Result.ConsumedGas
            };
        }

        public async Task Init(Block block, TransactionOperation parent, RawInternalDelegationResult content)
        {
            var sender = await Cache.Accounts.GetAsync(content.Source)
                ?? block.Originations?.FirstOrDefault(x => x.Contract.Address == content.Source)?.Contract;

            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);

            var delegat = Cache.Accounts.GetDelegateOrDefault(content.Delegate);
            
            Parent = parent;
            IsSelfDelegation = false;
            Delegation = new DelegationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Initiator = parent.Sender,
                Block = parent.Block,
                Level = parent.Block.Level,
                Timestamp = parent.Timestamp,
                OpHash = parent.OpHash,
                Counter = parent.Counter,
                Nonce = content.Nonce,
                Sender = sender,
                Delegate = delegat,
                PrevDelegate = sender.Delegate,
                Amount = sender.Balance,
                Status = content.Result.Status switch
                {
                    "applied" => OperationStatus.Applied,
                    "backtracked" => OperationStatus.Backtracked,
                    "failed" => OperationStatus.Failed,
                    "skipped" => OperationStatus.Skipped,
                    _ => throw new Exception($"Invalid status '{content.Result.Status}'")
                },
                Errors = OperationErrors.Parse(content.Result.Errors),
                GasUsed = content.Result.ConsumedGas
            };
        }

        public async Task Init(Block block, DelegationOperation delegation)
        {
            Delegation = delegation;
            
            Delegation.Block ??= block;
            Delegation.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            Delegation.Block.Baker ??= Cache.Accounts.GetDelegate(block.BakerId);
            
            Delegation.Sender = await Cache.Accounts.GetAsync(delegation.SenderId);
            Delegation.Sender.Delegate ??= Cache.Accounts.GetDelegate(delegation.Sender.DelegateId);
            Delegation.Delegate ??= Cache.Accounts.GetDelegate(delegation.DelegateId);
            Delegation.PrevDelegate ??= Cache.Accounts.GetDelegate(delegation.PrevDelegateId);

            if (Delegation.InitiatorId != null)
            {
                Delegation.Initiator = await Cache.Accounts.GetAsync(delegation.InitiatorId);
                Delegation.Initiator.Delegate ??= Cache.Accounts.GetDelegate(delegation.Initiator.DelegateId);
            }
        }

        public override async Task Apply()
        {
            if (Parent == null)
                await ApplyDelegation();
            else
                await ApplyInternalDelegation();
        }

        public override async Task Revert()
        {
            if (Delegation.InitiatorId == null)
                await RevertDelegation();
            else
                await RevertInternalDelegation();
        }

        public async Task ApplyDelegation()
        {
            #region entities
            var block = Delegation.Block;
            var blockBaker = block.Baker;

            var sender = Delegation.Sender;
            var prevDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            var newDelegate = Delegation.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);

            Db.TryAttach(sender);
            Db.TryAttach(prevDelegate);

            Db.TryAttach(newDelegate);
            #endregion

            #region apply operation
            await Spend(sender, Delegation.BakerFee);
            if (prevDelegate != null) prevDelegate.StakingBalance -= Delegation.BakerFee;
            blockBaker.FrozenFees += Delegation.BakerFee;
            blockBaker.Balance += Delegation.BakerFee;
            blockBaker.StakingBalance += Delegation.BakerFee;

            sender.DelegationsCount++;
            if (prevDelegate != null && prevDelegate != sender) prevDelegate.DelegationsCount++;
            if (newDelegate != null && newDelegate != sender && newDelegate != prevDelegate) newDelegate.DelegationsCount++;

            block.Operations |= Operations.Delegations;
            block.Fees += Delegation.BakerFee;

            sender.Counter = Math.Max(sender.Counter, Delegation.Counter);
            #endregion

            #region apply result
            if (Delegation.Status == OperationStatus.Applied)
            {
                if (IsSelfDelegation)
                {
                    if (sender.Type == AccountType.User)
                    {
                        await ResetDelegate(sender, prevDelegate);
                        await UpgradeUser(Delegation);

                        #region weird delegators
                        var delegat = (Data.Models.Delegate)Delegation.Sender;

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

                                await SetDelegate(origination.Contract, delegat, origination.Level);
                            }
                        }
                        #endregion
                    }
                    else if (sender is Data.Models.Delegate delegat)
                    {
                        Delegation.ResetDeactivation = delegat.DeactivationLevel;
                        await ReactivateDelegate(Delegation);
                    }
                }
                else
                {
                    await ResetDelegate(sender, prevDelegate);
                    if (newDelegate != null)
                        await SetDelegate(sender, newDelegate, Delegation.Block.Level);
                }
            }
            #endregion

            Db.DelegationOps.Add(Delegation);
        }

        public async Task ApplyInternalDelegation()
        {
            #region entities
            var block = Delegation.Block;
            var parentTx = Parent;
            var parentSender = parentTx.Sender;

            var sender = Delegation.Sender;
            var prevDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            var newDelegate = Delegation.Delegate;

            //Db.TryAttach(block);

            Db.TryAttach(sender);
            Db.TryAttach(parentSender);
            Db.TryAttach(prevDelegate);
            Db.TryAttach(newDelegate);
            #endregion

            #region apply operation
            parentTx.InternalOperations = (parentTx.InternalOperations ?? InternalOperations.None) | InternalOperations.Delegations;

            sender.DelegationsCount++;
            if (prevDelegate != null && prevDelegate != sender) prevDelegate.DelegationsCount++;
            if (newDelegate != null && newDelegate != sender && newDelegate != prevDelegate) newDelegate.DelegationsCount++;
            if (parentSender != sender && parentSender != prevDelegate && parentSender != newDelegate) parentSender.DelegationsCount++;

            block.Operations |= Operations.Delegations;
            #endregion

            #region apply result
            if (Delegation.Status == OperationStatus.Applied)
            {
                await ResetDelegate(sender, prevDelegate);
                if (newDelegate != null)
                    await SetDelegate(sender, newDelegate, Delegation.Block.Level);
            }
            #endregion

            Db.DelegationOps.Add(Delegation);
        }

        public async Task RevertDelegation()
        {
            #region entities
            var block = Delegation.Block;
            var blockBaker = block.Baker;

            var sender = Delegation.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var newDelegate = Delegation.Delegate;

            var prevDelegation = await GetPrevDelegationAsync(sender, Delegation.Id);
            var prevDelegationLevel = prevDelegation?.Level;
            var prevDelegate = prevDelegation?.Delegate;

            if (prevDelegation == null)
            {
                if (sender is Contract contract)
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
            if (Delegation.Status == OperationStatus.Applied)
            {
                Delegation.Sender = sender;
                Delegation.Delegate = newDelegate;

                if (sender.Id == newDelegate?.Id)
                {
                    if (Delegation.ResetDeactivation == null)
                    {
                        #region weird delegations
                        var delegat = (Data.Models.Delegate)Delegation.Sender;

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

                                await ResetDelegate(origination.Contract, delegat);
                            }
                        }
                        #endregion

                        await ResetDelegate(sender, senderDelegate);
                        await DowngradeDelegate(Delegation);

                        sender = Delegation.Sender;

                        if (prevDelegate != null && prevDelegate.Id != sender.Id)
                            await SetDelegate(sender, prevDelegate, (int)prevDelegationLevel);
                    }
                    else
                    {
                        await DeactivateDelegate(Delegation, (int)Delegation.ResetDeactivation);
                    }
                }
                else
                {
                    await ResetDelegate(sender, senderDelegate);
                    if (prevDelegate != null && prevDelegate.Id != sender.Id)
                        await SetDelegate(sender, prevDelegate, (int)prevDelegationLevel);
                }
            }
            #endregion

            #region revert operation
            await Return(sender, Delegation.BakerFee);
            if (prevDelegate != null) prevDelegate.StakingBalance += Delegation.BakerFee;
            blockBaker.FrozenFees -= Delegation.BakerFee;
            blockBaker.Balance -= Delegation.BakerFee;
            blockBaker.StakingBalance -= Delegation.BakerFee;

            sender.DelegationsCount--;
            if (prevDelegate != null && prevDelegate != sender) prevDelegate.DelegationsCount--;
            if (newDelegate != null && newDelegate != sender && newDelegate != prevDelegate) newDelegate.DelegationsCount--;

            sender.Counter = Math.Min(sender.Counter, Delegation.Counter - 1);
            #endregion

            Db.DelegationOps.Remove(Delegation);
            Cache.AppState.ReleaseManagerCounter();
        }

        public async Task RevertInternalDelegation()
        {
            #region entities
            var block = Delegation.Block;

            var sender = Delegation.Sender;
            var parentSender = Delegation.Initiator;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var newDelegate = Delegation.Delegate;

            var prevDelegation = await GetPrevDelegationAsync(sender, Delegation.Id);
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

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(newDelegate);
            Db.TryAttach(prevDelegate);
            #endregion

            #region revert result
            if (Delegation.Status == OperationStatus.Applied)
            {
                await ResetDelegate(sender, senderDelegate);
                if (prevDelegate != null)
                    await SetDelegate(sender, prevDelegate, (int)prevDelegationLevel);
            }
            #endregion

            #region revert operation
            sender.DelegationsCount--;
            if (prevDelegate != null && prevDelegate != sender) prevDelegate.DelegationsCount--;
            if (newDelegate != null && newDelegate != sender && newDelegate != prevDelegate) newDelegate.DelegationsCount--;
            if (parentSender != sender && parentSender != prevDelegate && parentSender != newDelegate) parentSender.DelegationsCount--;
            #endregion

            Db.DelegationOps.Remove(Delegation);
        }


        Task UpgradeUser(DelegationOperation delegation)
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
                ContractsCount = user.ContractsCount,
                MigrationsCount = user.MigrationsCount,
                PublicKey = user.PublicKey,
                Revealed = user.Revealed,
                Staked = true,
                StakingBalance = user.Balance,
                Type = AccountType.Delegate,
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

                            if (op.Manager?.Id == user.Id)
                                op.Manager = delegat;

                            if (op.Initiator?.Id == user.Id)
                                op.Initiator = delegat;

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
                    case TransactionOperation op:
                        if (op.Sender?.Id == user.Id || op.Target?.Id == user.Id || op.Initiator?.Id == user.Id)
                        {
                            if (op.Sender?.Id == user.Id)
                                op.Sender = delegat;

                            if (op.Target?.Id == user.Id)
                                op.Target = delegat;

                            if (op.Initiator?.Id == user.Id)
                                op.Initiator = delegat;

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

            Db.Entry(user).State = EntityState.Detached;
            Db.Entry(delegat).State = EntityState.Modified;
            Cache.Accounts.Add(delegat);

            #region update graph
            foreach (var (entry, state) in touched)
                Db.Entry(entry).State = state;
            #endregion

            delegation.Sender = delegation.Delegate = delegat;
            return Task.CompletedTask;
        }

        Task DowngradeDelegate(DelegationOperation delegation)
        {
            var delegat = delegation.Delegate as Data.Models.Delegate;

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
                ContractsCount = delegat.ContractsCount,
                MigrationsCount = delegat.MigrationsCount,
                PublicKey = delegat.PublicKey,
                Revealed = delegat.Revealed,
                Staked = false,
                Type = AccountType.User,
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

                            if (op.Delegate?.Id == delegat.Id)
                                op.Delegate = null;

                            if (op.Initiator?.Id == delegat.Id)
                                op.Initiator = user;

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

                            if (op.Manager?.Id == delegat.Id)
                                op.Manager = user;

                            if (op.Delegate?.Id == delegat.Id)
                                op.Delegate = null;

                            if (op.Initiator?.Id == delegat.Id)
                                op.Initiator = user;

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
                    case TransactionOperation op:
                        if (op.Sender?.Id == delegat.Id || op.Target?.Id == delegat.Id || op.Initiator?.Id == delegat.Id)
                        {
                            if (op.Sender?.Id == delegat.Id)
                                op.Sender = user;

                            if (op.Target?.Id == delegat.Id)
                                op.Target = user;

                            if (op.Initiator?.Id == delegat.Id)
                                op.Initiator = user;

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

            Db.Entry(delegat).State = EntityState.Detached;
            Db.Entry(user).State = EntityState.Modified;
            Cache.Accounts.Add(user);

            #region update graph
            foreach (var (entry, state) in touched)
                Db.Entry(entry).State = state;
            #endregion

            delegation.Sender = user;
            delegation.Delegate = null;
            return Task.CompletedTask;
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


        Task SetDelegate(Account sender, Data.Models.Delegate newDelegate, int level)
        {
            sender.Delegate = newDelegate;
            sender.DelegateId = newDelegate.Id;
            sender.DelegationLevel = level;
            sender.Staked = newDelegate.Staked;

            newDelegate.DelegatorsCount++;
            newDelegate.StakingBalance += sender.Balance;

            return Task.CompletedTask;
        }

        Task ResetDelegate(Account sender, Data.Models.Delegate currentDelegate)
        {
            if (currentDelegate != null)
            {
                if (sender.Address != currentDelegate.Address)
                    currentDelegate.DelegatorsCount--;
                
                currentDelegate.StakingBalance -= sender.Balance;
            }

            sender.Delegate = null;
            sender.DelegateId = null;
            sender.DelegationLevel = null;
            sender.Staked = false;

            return Task.CompletedTask;
        }


        async Task<OriginationOperation> GetOriginationAsync(Contract contract)
        {
            var result = await Db.OriginationOps
                .FirstAsync(x => x.Status == OperationStatus.Applied && x.ContractId == contract.Id);

            result.Delegate ??= Cache.Accounts.GetDelegate(result.DelegateId);

            return result;
        }

        async Task<DelegationOperation> GetPrevDelegationAsync(Account sender, int id)
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

        #region static
        public static async Task<DelegationsCommit> Apply(ProtocolHandler proto, Block block, RawOperation op, RawDelegationContent content)
        {
            var commit = new DelegationsCommit(proto);
            await commit.Init(block, op, content);
            await commit.Apply();

            return commit;
        }

        public static async Task<DelegationsCommit> Apply(ProtocolHandler proto, Block block, TransactionOperation parent, RawInternalDelegationResult content)
        {
            var commit = new DelegationsCommit(proto);
            await commit.Init(block, parent, content);
            await commit.Apply();

            return commit;
        }

        public static async Task<DelegationsCommit> Revert(ProtocolHandler proto, Block block, DelegationOperation op)
        {
            var commit = new DelegationsCommit(proto);
            await commit.Init(block, op);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
