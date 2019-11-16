using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto2
{
    class DelegationsCommit : ProtocolCommit
    {
        public DelegationOperation Delegation { get; private set; }
        public bool IsSelfDelegation { get; private set; }

        DelegationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawDelegationContent content)
        {
            var sender = await Cache.GetAccountAsync(content.Source);
            sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(sender.DelegateId);

            var delegat = await Cache.GetDelegateOrDefaultAsync(content.Delegate);

            IsSelfDelegation = content.Source == content.Delegate;
            Delegation = new DelegationOperation
            {
                Id = await Cache.NextCounterAsync(),
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
                Status = content.Metadata.Result.Status switch
                {
                    "applied" => OperationStatus.Applied,
                    "failed" => OperationStatus.Failed,
                    _ => throw new NotImplementedException()
                }
            };
        }

        public async Task Init(Block block, DelegationOperation delegation)
        {
            Delegation = delegation;
            
            Delegation.Block ??= block;
            Delegation.Block.Protocol ??= await Cache.GetProtocolAsync(block.ProtoCode);
            Delegation.Block.Baker ??= (Data.Models.Delegate)await Cache.GetAccountAsync(block.BakerId);
            
            Delegation.Sender = await Cache.GetAccountAsync(delegation.SenderId);
            Delegation.Sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(delegation.Sender.DelegateId);
            Delegation.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(delegation.DelegateId);
        }

        public override async Task Apply()
        {
            #region entities
            var block = Delegation.Block;
            var blockBaker = block.Baker;

            var sender = Delegation.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var newDelegate = Delegation.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(newDelegate);
            #endregion

            #region apply operation
            sender.Balance -= Delegation.BakerFee;
            if (senderDelegate != null) senderDelegate.StakingBalance -= Delegation.BakerFee;
            blockBaker.FrozenFees += Delegation.BakerFee;
            blockBaker.Balance += Delegation.BakerFee;
            blockBaker.StakingBalance += Delegation.BakerFee;

            sender.Operations |= Operations.Delegations;
            block.Operations |= Operations.Delegations;

            sender.Counter = Math.Max(sender.Counter, Delegation.Counter);
            #endregion

            #region apply result
            if (Delegation.Status == OperationStatus.Applied)
            {
                if (IsSelfDelegation)
                {
                    if (sender.Type == AccountType.User)
                    {
                        await ResetDelegate(sender, senderDelegate);
                        await UpgradeUser(Delegation);
                    }
                    else if (sender is Data.Models.Delegate delegat)
                    {
                        Delegation.ResetDeactivation = delegat.DeactivationLevel;
                        await ReactivateDelegate(Delegation);
                    }
                }
                else
                {
                    await ResetDelegate(sender, senderDelegate);
                    if (newDelegate != null)
                        await SetDelegate(sender, newDelegate, Delegation.Block.Level);
                }
            }
            #endregion

            Db.DelegationOps.Add(Delegation);
        }

        public override async Task Revert()
        {
            #region entities
            var block = Delegation.Block;
            var blockBaker = block.Baker;

            var sender = Delegation.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var newDelegate = Delegation.Delegate;

            var prevDelegation = await GetPrevDelegationAsync(sender, Delegation.Counter);
            var prevDelegationLevel = prevDelegation?.Level;
            var prevDelegate = prevDelegation?.Delegate;

            if (prevDelegation == null && sender is Contract contract)
            {
                if (contract.WeirdDelegateId != null)
                {
                    prevDelegate = await Cache.GetAccountAsync(contract.WeirdDelegateId) as Data.Models.Delegate;
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

                        var weirdDelegators = await Db.Contracts
                            .Where(x => x.WeirdDelegateId == delegat.Id && !x.Operations.HasFlag(Operations.Delegations))
                            .ToListAsync();

                        foreach (var weirdDelegator in weirdDelegators)
                        {
                            Db.TryAttach(weirdDelegator);
                            Cache.AddAccount(weirdDelegator);

                            await ResetDelegate(weirdDelegator, delegat);
                        }
                        #endregion

                        await ResetDelegate(sender, senderDelegate);
                        await DowngradeDelegate(Delegation);

                        sender = Delegation.Sender;

                        if (prevDelegate != null)
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
                    if (prevDelegate != null)
                        await SetDelegate(sender, prevDelegate, (int)prevDelegationLevel);
                }
            }
            #endregion

            #region revert operation
            sender.Balance += Delegation.BakerFee;
            if (prevDelegate != null) prevDelegate.StakingBalance += Delegation.BakerFee;
            blockBaker.FrozenFees -= Delegation.BakerFee;
            blockBaker.Balance -= Delegation.BakerFee;
            blockBaker.StakingBalance -= Delegation.BakerFee;

            if (!await Db.DelegationOps.AnyAsync(x => x.SenderId == Delegation.SenderId && x.Id < Delegation.Id))
                Delegation.Sender.Operations &= ~Operations.Delegations;

            sender.Counter = Math.Min(sender.Counter, Delegation.Counter - 1);
            #endregion

            Db.DelegationOps.Remove(Delegation);
            await Cache.ReleaseCounterAsync(true);
        }

        Task UpgradeUser(DelegationOperation delegation)
        {
            var user = delegation.Sender as User;

            var delegat = new Data.Models.Delegate
            {
                ActivationLevel = delegation.Level,
                Address = user.Address,
                AirDrop = user.AirDrop,
                FirstLevel = user.FirstLevel,
                LastLevel = user.LastLevel,
                Balance = user.Balance,
                Counter = user.Counter,
                DeactivationLevel = GracePeriod.Init(delegation.Block),
                Delegate = null,
                DelegateId = null,
                DelegationLevel = null,
                Id = user.Id,
                Operations = user.Operations,
                PublicKey = user.PublicKey,
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
                        if (op.Sender?.Id == user.Id)
                        {
                            op.Sender = delegat;
                            touched.Add((op, entry.State));
                        }
                        break;
                    case OriginationOperation op:
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
                    case TransactionOperation op:
                        if (op.Sender?.Id == user.Id || op.Target?.Id == user.Id)
                        {
                            if (op.Sender?.Id == user.Id)
                                op.Sender = delegat;

                            if (op.Target?.Id == user.Id)
                                op.Target = delegat;

                            touched.Add((op, entry.State));
                        }
                        break;
                    case Contract contract:
                        if (contract.WeirdDelegate?.Id == user.Id || contract.Manager?.Id == user.Id)
                        {
                            if (contract.WeirdDelegate?.Id == user.Id)
                                contract.WeirdDelegate = delegat;

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
            Cache.AddAccount(delegat);

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
                AirDrop = delegat.AirDrop,
                FirstLevel = delegat.FirstLevel,
                LastLevel = delegat.LastLevel,
                Balance = delegat.Balance,
                Counter = delegat.Counter,
                Delegate = null,
                DelegateId = null,
                DelegationLevel = null,
                Id = delegat.Id,
                Operations = delegat.Operations,
                PublicKey = delegat.PublicKey,
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
                        if (op.Sender?.Id == delegat.Id || op.Delegate?.Id == delegat.Id)
                        {
                            if (op.Sender?.Id == delegat.Id)
                                op.Sender = user;

                            if (op.Delegate?.Id == delegat.Id)
                                op.Delegate = null;

                            touched.Add((op, entry.State));
                        }
                        break;
                    case OriginationOperation op:
                        if (op.Sender?.Id == delegat.Id || op.Delegate?.Id == delegat.Id)
                        {
                            if (op.Sender?.Id == delegat.Id)
                                op.Sender = user;

                            if (op.Delegate?.Id == delegat.Id)
                                op.Delegate = null;

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
                        if (op.Sender?.Id == delegat.Id || op.Target?.Id == delegat.Id)
                        {
                            if (op.Sender?.Id == delegat.Id)
                                op.Sender = user;

                            if (op.Target?.Id == delegat.Id)
                                op.Target = user;

                            touched.Add((op, entry.State));
                        }
                        break;
                    case Contract contract:
                        if (contract.WeirdDelegate?.Id == delegat.Id || contract.Manager?.Id == delegat.Id)
                        {
                            if (contract.WeirdDelegate?.Id == delegat.Id)
                                contract.WeirdDelegate = user;

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
            Cache.AddAccount(user);

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
                Cache.AddAccount(delegator);
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
                Cache.AddAccount(delegator);
                delegator.Staked = false;
            }
        }


        Task SetDelegate(Account sender, Data.Models.Delegate newDelegate, int level)
        {
            sender.Delegate = newDelegate;
            sender.DelegateId = newDelegate.Id;
            sender.DelegationLevel = level;
            sender.Staked = true;

            newDelegate.Delegators++;
            newDelegate.StakingBalance += sender.Balance;

            return Task.CompletedTask;
        }

        Task ResetDelegate(Account sender, Data.Models.Delegate currentDelegate)
        {
            if (currentDelegate != null)
            {
                if (sender.Address != currentDelegate.Address)
                    currentDelegate.Delegators--;
                
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

            result.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(result.DelegateId);

            return result;
        }

        async Task<DelegationOperation> GetPrevDelegationAsync(Account sender, int counter)
        {
            var result = await Db.DelegationOps
                .Where(x => x.Status == OperationStatus.Applied &&
                    x.SenderId == sender.Id &&
                    x.Counter < counter)
                .OrderByDescending(x => x.Counter)
                .FirstOrDefaultAsync();

            if (result != null)
            {
                result.Sender = sender;
                result.Delegate = (Data.Models.Delegate)await Cache.GetAccountAsync(result.DelegateId);
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
