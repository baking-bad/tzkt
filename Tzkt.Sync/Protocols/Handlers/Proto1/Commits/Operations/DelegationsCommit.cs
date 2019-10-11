using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto1
{
    class DelegationsCommit : ProtocolCommit
    {
        #region constants
        const int BlocksPerCycle = 4096;
        #endregion

        public List<DelegationOperation> Delegations { get; private set; }
        public HashSet<string> Activations { get; private set; }

        public DelegationsCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;
            var parsedBlock = FindCommit<BlockCommit>().Block;

            Delegations = new List<DelegationOperation>();
            Activations = new HashSet<string>();
            foreach (var op in rawBlock.Operations[3])
            {
                foreach (var content in op.Contents.Where(x => x is RawDelegationContent))
                {
                    var delegation = content as RawDelegationContent;

                    if (delegation.Source == delegation.Delegate)
                        Activations.Add(delegation.Source);

                    Delegations.Add(new DelegationOperation
                    {
                        Block = parsedBlock,
                        Timestamp = parsedBlock.Timestamp,

                        OpHash = op.Hash,

                        BakerFee = delegation.Fee,
                        Counter = delegation.Counter,
                        GasLimit = delegation.GasLimit,
                        StorageLimit = delegation.StorageLimit,
                        Sender = await Accounts.GetAccountAsync(delegation.Source),
                        Delegate = delegation.Delegate != null && delegation.Source != delegation.Delegate
                           ? (Data.Models.Delegate)await Accounts.GetAccountAsync(delegation.Delegate)
                           : null,

                        Status = delegation.Metadata.Result.Status switch
                        {
                            "applied" => OperationStatus.Applied,
                            _ => throw new NotImplementedException()
                        }
                    });
                }
            }
        }

        public override async Task Apply()
        {
            if (Delegations == null)
                throw new Exception("Commit is not initialized");

            foreach (var op in Delegations)
            {
                #region apply operation
                op.Sender.Balance -= op.BakerFee;
                op.Sender.Counter = Math.Max(op.Sender.Counter, op.Counter);
                op.Sender.Operations |= Operations.Delegations;

                var delegat = op.Sender.Delegate ?? op.Sender as Data.Models.Delegate;
                if (delegat != null)
                    delegat.StakingBalance -= op.BakerFee;

                op.Block.Baker.FrozenFees += op.BakerFee;
                op.Block.Operations |= Operations.Delegations;
                #endregion

                if (op.Status == OperationStatus.Applied)
                {
                    #region apply result
                    await ResetDelegate(op);

                    if (Activations.Contains(op.Sender.Address))
                    {
                        if (op.Sender is User user)
                        {
                            await UpgradeUser(op);
                        }
                        else if (op.Sender is Data.Models.Delegate)
                        {
                            await ReactivateDelegate(op);
                        }
                    }
                    else if (op.Delegate != null)
                    {
                        await SetDelegate(op);
                    }
                    #endregion
                }

                if (delegat != null)
                    Db.Delegates.Update(delegat);

                if (op.Sender.Delegate != null)
                    Db.Delegates.Update(op.Sender.Delegate);

                Db.Update(op.Sender);
                Db.Delegates.Update(op.Block.Baker);

                Db.DelegationOps.Add(op);
            }
        }

        public override async Task Revert()
        {
            if (Delegations == null)
                throw new Exception("Commit is not initialized");

            foreach (var op in Delegations)
            {
                if (op.Status == OperationStatus.Applied)
                {
                    op.Sender = await Accounts.GetAccountAsync(op.SenderId);
                    op.Delegate = op.DelegateId != null
                        ? (Data.Models.Delegate)await Accounts.GetAccountAsync((int)op.DelegateId)
                        : null;

                    #region revert result
                    await ResetDelegate(op);

                    if (op.Sender.Address == op.Delegate.Address)
                    {
                        var prevActivation = await GetPrevActivationAsync(op);
                        if (prevActivation == null)
                        {
                            await DowngradeDelegate(op);

                            var prevDelegation = await GetPrevDelegationAsync(op);
                            if (prevDelegation?.Delegate != null)
                                await SetDelegate(prevDelegation);
                        }
                        else
                        {
                            var prevState = await GetDelegateAsync(op.Level - 1, op.Sender.Address);
                            if (prevState.Deactivated)
                                await DeactivateDelegate(op, (prevState.GracePeriod + 1) * BlocksPerCycle);
                        }
                    }
                    else
                    {
                        var prevDelegation = await GetPrevDelegationAsync(op);
                        if (prevDelegation?.Delegate != null)
                            await SetDelegate(prevDelegation);
                    }
                    #endregion
                }

                #region revert operation
                op.Sender.Balance += op.BakerFee;
                op.Sender.Counter = Math.Min(op.Sender.Counter, op.Counter - 1);
                if (!await Db.DelegationOps.AnyAsync(x => x.Sender.Id == op.SenderId && x.Id != op.Id))
                    op.Sender.Operations &= ~Operations.Delegations;

                var delegat = op.Sender.Delegate ?? op.Sender as Data.Models.Delegate;
                if (delegat != null)
                    delegat.StakingBalance += op.BakerFee;

                op.Block.Baker.FrozenFees -= op.BakerFee;
                #endregion

                if (delegat != null)
                    Db.Delegates.Update(delegat);

                if (op.Sender.Delegate != null)
                    Db.Delegates.Update(op.Sender.Delegate);

                Db.Update(op.Sender);
                Db.Delegates.Update(op.Block.Baker);

                Db.DelegationOps.Remove(op);
            }
        }

        Task UpgradeUser(DelegationOperation delegation)
        {
            var user = delegation.Sender as User;

            var delegat = new Data.Models.Delegate
            {
                ActivationBlock = delegation.Block,
                ActivationLevel = delegation.Level,
                Activation = user.Activation,
                Address = user.Address,
                Balance = user.Balance,
                Counter = user.Counter,
                DeactivationBlock = null,
                DeactivationLevel = null,
                Delegate = null,
                DelegateId = null,
                DelegationLevel = null,
                Id = user.Id,
                ManagedContracts = user.ManagedContracts,
                ManagedOriginations = user.ManagedOriginations,
                Operations = user.Operations,
                OriginatedContracts = user.OriginatedContracts,
                PublicKey = user.PublicKey,
                ReceivedTransactions = user.ReceivedTransactions,
                Reveal = user.Reveal,
                SentDelegations = user.SentDelegations,
                SentOriginations = user.SentOriginations,
                SentTransactions = user.SentTransactions,
                Staked = true,
                StakingBalance = user.Balance,
                Type = AccountType.Delegate
            };

            #region update relations
            foreach (var op in FindCommit<RevealsCommit>().Reveals)
            {
                if (op.SenderId == user.Id)
                    op.Sender = delegat;
            }

            foreach (var op in Delegations)
            {
                if (op.SenderId == user.Id)
                    op.Sender = delegat;

                if (op.DelegateId == user.Id)
                    op.Delegate = delegat;
            }
            #endregion

            Accounts.ChangeAccountType(user, delegat, FindCommit<RevealsCommit>().Reveals);

            #region update graph
            foreach (var op in FindCommit<RevealsCommit>().Reveals
                .Where(x => x.SenderId == user.Id))
                Db.RevealOps.Update(op);

            foreach (var op in FindCommit<DelegationsCommit>().Delegations
                .Where(x => x.SenderId == user.Id || x.DelegateId == user.Id))
                Db.DelegationOps.Update(op);
            #endregion

            delegation.Sender = delegation.Delegate = delegat;
            return Task.CompletedTask;
        }

        Task DowngradeDelegate(DelegationOperation delegation)
        {
            var delegat = delegation.Delegate as Data.Models.Delegate;

            var user = new User
            {
                Activation = delegat.Activation,
                Address = delegat.Address,
                Balance = delegat.Balance,
                Counter = delegat.Counter,
                Delegate = null,
                DelegateId = null,
                DelegationLevel = null,
                Id = delegat.Id,
                ManagedContracts = delegat.ManagedContracts,
                ManagedOriginations = delegat.ManagedOriginations,
                Operations = delegat.Operations,
                OriginatedContracts = delegat.OriginatedContracts,
                PublicKey = delegat.PublicKey,
                ReceivedTransactions = delegat.ReceivedTransactions,
                Reveal = delegat.Reveal,
                SentDelegations = delegat.SentDelegations,
                SentOriginations = delegat.SentOriginations,
                SentTransactions = delegat.SentTransactions,
                Staked = false,
                Type = AccountType.User
            };

            #region update relations
            foreach (var op in FindCommit<RevealsCommit>().Reveals)
            {
                if (op.SenderId == delegat.Id)
                    op.Sender = user;
            }

            foreach (var op in FindCommit<DelegationsCommit>().Delegations)
            {
                if (op.SenderId == delegat.Id)
                    op.Sender = user;

                if (op.DelegateId == delegat.Id)
                    op.Delegate = null;
            }

            foreach (var op in FindCommit<TransactionsCommit>().Transactions)
            {
                if (op.SenderId == delegat.Id)
                    op.Sender = user;

                if (op.TargetId == delegat.Id)
                    op.Target = user;
            }

            foreach (var op in FindCommit<OriginationsCommit>().Originations)
            {
                if (op.SenderId == delegat.Id)
                    op.Sender = user;

                if (op.DelegateId == delegat.Id)
                    op.Delegate = null;

                if (op.ManagerId == delegat.Id)
                    op.Manager = user;
            }
            #endregion

            Accounts.ChangeAccountType(delegat, user, null);

            #region update graph
            foreach (var op in FindCommit<RevealsCommit>().Reveals
                .Where(x => x.SenderId == user.Id))
                Db.RevealOps.Update(op);

            foreach (var op in FindCommit<DelegationsCommit>().Delegations
                .Where(x => x.SenderId == user.Id || x.DelegateId == null && x.Sender is User))
                Db.DelegationOps.Update(op);

            foreach (var op in FindCommit<TransactionsCommit>().Transactions
                .Where(x => x.SenderId == user.Id || x.TargetId == user.Id))
                Db.TransactionOps.Update(op);

            foreach (var op in FindCommit<OriginationsCommit>().Originations
                .Where(x => x.SenderId == user.Id || x.DelegateId == null && x.Sender is User || x.ManagerId == user.Id))
                Db.OriginationOps.Update(op);
            #endregion

            delegation.Sender = user;
            delegation.Delegate = null;
            return Task.CompletedTask;
        }

        async Task ReactivateDelegate(DelegationOperation delegation)
        {
            var delegat = delegation.Sender as Data.Models.Delegate;

            delegat.DeactivationBlock = null;
            delegat.DeactivationLevel = null;
            delegat.Staked = true;

            foreach (var delegator in await Db.Accounts.Where(x => x.DelegateId == delegat.Id).ToListAsync())
                delegator.Staked = true;

            delegation.Delegate = delegat;
        }

        async Task DeactivateDelegate(DelegationOperation delegation, int deactivationLevel)
        {
            var delegat = delegation.Sender as Data.Models.Delegate;

            var deactivationBlock = await Db.Blocks.FirstOrDefaultAsync(x => x.Level == deactivationLevel);
            delegat.DeactivationBlock = deactivationBlock;
            delegat.DeactivationLevel = deactivationBlock.Level;
            delegat.Staked = false;

            foreach (var delegator in await Db.Accounts.Where(x => x.DelegateId == delegat.Id).ToListAsync())
                delegator.Staked = false;
        }

        Task SetDelegate(DelegationOperation delegation)
        {
            delegation.Sender.Delegate = delegation.Delegate;
            delegation.Sender.DelegateId = delegation.DelegateId;
            delegation.Sender.DelegationLevel = delegation.Level;
            delegation.Sender.Staked = true;

            delegation.Sender.Delegate.Delegators++;
            delegation.Sender.Delegate.StakingBalance += delegation.Sender.Balance;

            return Task.CompletedTask;
        }

        Task ResetDelegate(DelegationOperation delegation)
        {
            if (delegation.Sender.Delegate != null)
            {
                delegation.Sender.Delegate.Delegators--;
                delegation.Sender.Delegate.StakingBalance -= delegation.Sender.Balance;
            }

            delegation.Sender.Delegate = null;
            delegation.Sender.DelegateId = null;
            delegation.Sender.DelegationLevel = null;
            delegation.Sender.Staked = false;

            return Task.CompletedTask;
        }

        Task<DelegationOperation> GetPrevDelegationAsync(DelegationOperation delegation)
        {
            return Db.DelegationOps
                .Include(x => x.Delegate)
                .Where(x => x.Status == OperationStatus.Applied &&
                    x.SenderId == delegation.SenderId &&
                    x.Counter < delegation.Counter)
                .OrderByDescending(x => x.Counter)
                .FirstOrDefaultAsync();
        }

        Task<DelegationOperation> GetPrevActivationAsync(DelegationOperation delegation)
        {
            return Db.DelegationOps
                .Include(x => x.Delegate)
                .Where(x => x.Status == OperationStatus.Applied &&
                    x.SenderId == delegation.SenderId &&
                    x.DelegateId == delegation.SenderId &&
                    x.Counter < delegation.Counter)
                .OrderByDescending(x => x.Counter)
                .FirstOrDefaultAsync();
        }

        async Task<RawDelegate> GetDelegateAsync(int level, string address)
        {
            var stream = await Proto.Node.GetDelegateAsync(level, address);
            return await (Proto.Serializer as Serializer).DeserializeDelegate(stream);
        }

        #region static
        public static async Task<DelegationsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new DelegationsCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static Task<DelegationsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, List<DelegationOperation> delegations)
        {
            var commit = new DelegationsCommit(protocol, commits) { Delegations = delegations };
            return Task.FromResult(commit);
        }
        #endregion
    }
}
