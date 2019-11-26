using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto5
{
    class TransactionsCommit : ProtocolCommit
    {
        public TransactionOperation Transaction { get; private set; }

        TransactionsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawTransactionContent content)
        {
            var sender = await Cache.GetAccountAsync(content.Source);
            sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(sender.DelegateId);

            var target = await Cache.GetAccountAsync(content.Destination);

            if (target != null)
                target.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(target.DelegateId);

            Transaction = new TransactionOperation
            {
                Id = await Cache.NextCounterAsync(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.Hash,
                Amount = content.Amount,
                BakerFee = content.Fee,
                Counter = content.Counter,
                GasLimit = content.GasLimit,
                StorageLimit = content.StorageLimit,
                Sender = sender,
                Target = target,
                Status = content.Metadata.Result.Status switch
                {
                    "applied" => OperationStatus.Applied,
                    "backtracked" => OperationStatus.Backtracked,
                    "failed" => OperationStatus.Failed,
                    "skipped" => OperationStatus.Skipped,
                    _ => throw new Exception($"Invalid status '{content.Metadata.Result.Status}'")
                },
                GasUsed = content.Metadata.Result.ConsumedGas,
                StorageUsed = content.Metadata.Result.PaidStorageSizeDiff,
                StorageFee = content.Metadata.Result.PaidStorageSizeDiff > 0
                    ? (int?)(content.Metadata.Result.PaidStorageSizeDiff * block.Protocol.ByteCost)
                    : null,
                AllocationFee = content.Metadata.Result.AllocatedDestinationContract
                    ? (int?)(block.Protocol.OriginationSize * block.Protocol.ByteCost)
                    : null
            };
        }

        public async Task Init(Block block, TransactionOperation parent, RawInternalTransactionResult content)
        {
            var id = await Cache.NextCounterAsync();

            var sender = await Cache.GetAccountAsync(content.Source);
            sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(sender.DelegateId);

            var target = await Cache.GetAccountAsync(content.Destination);

            if (target != null)
                target.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(target.DelegateId);

            Transaction = new TransactionOperation
            {
                Id = id,
                Parent = parent,
                Block = parent.Block,
                Level = parent.Block.Level,
                Timestamp = parent.Timestamp,
                OpHash = parent.OpHash,
                Counter = parent.Counter,
                Amount = content.Amount,
                Nonce = content.Nonce,
                Sender = sender,
                Target = target,
                Status = content.Result.Status switch
                {
                    "applied" => OperationStatus.Applied,
                    "backtracked" => OperationStatus.Backtracked,
                    "failed" => OperationStatus.Failed,
                    "skipped" => OperationStatus.Skipped,
                    _ => throw new Exception($"Invalid status '{content.Result.Status}'")
                },
                GasUsed = content.Result.ConsumedGas,
                StorageUsed = content.Result.PaidStorageSizeDiff,
                StorageFee = content.Result.PaidStorageSizeDiff > 0
                    ? (int?)(content.Result.PaidStorageSizeDiff * block.Protocol.ByteCost)
                    : null,
                AllocationFee = content.Result.AllocatedDestinationContract
                    ? (int?)(block.Protocol.OriginationSize * block.Protocol.ByteCost)
                    : null
            };
        }

        public async Task Init(Block block, TransactionOperation transaction)
        {
            Transaction = transaction;

            Transaction.Block ??= block;
            Transaction.Block.Protocol ??= await Cache.GetProtocolAsync(block.ProtoCode);
            Transaction.Block.Baker ??= (Data.Models.Delegate)await Cache.GetAccountAsync(block.BakerId);

            Transaction.Sender = await Cache.GetAccountAsync(transaction.SenderId);
            Transaction.Sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(transaction.Sender.DelegateId);
            Transaction.Target = await Cache.GetAccountAsync(transaction.TargetId);

            if (Transaction.Target != null)
                Transaction.Target.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(transaction.Target.DelegateId);

            if (Transaction.Parent != null)
            {
                Transaction.Parent.Sender = await Cache.GetAccountAsync(transaction.Parent.SenderId);
                Transaction.Parent.Sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(transaction.Parent.Sender.DelegateId);
            }
        }

        public override async Task Apply()
        {
            if (Transaction.Parent == null)
                await ApplyTransaction();
            else
                await ApplyInternalTransaction();
        }

        public override async Task Revert()
        {
            if (Transaction.ParentId == null)
                await RevertTransaction();
            else
                await RevertInternalTransaction();
        }

        public async Task ApplyTransaction()
        {
            #region entities
            var block = Transaction.Block;
            var blockBaker = block.Baker;

            var sender = Transaction.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var target = Transaction.Target;
            var targetDelegate = target?.Delegate ?? target as Data.Models.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(target);
            Db.TryAttach(targetDelegate);
            #endregion

            #region apply operation
            sender.Balance -= Transaction.BakerFee;
            if (senderDelegate != null) senderDelegate.StakingBalance -= Transaction.BakerFee;
            blockBaker.FrozenFees += Transaction.BakerFee;
            blockBaker.Balance += Transaction.BakerFee;
            blockBaker.StakingBalance += Transaction.BakerFee;

            sender.TransactionsCount++;
            if (target != null) target.TransactionsCount++;

            block.Operations |= Operations.Transactions;

            sender.Counter = Math.Max(sender.Counter, Transaction.Counter);
            #endregion

            #region apply result
            if (Transaction.Status == OperationStatus.Applied)
            {
                sender.Balance -= Transaction.Amount;
                sender.Balance -= Transaction.StorageFee ?? 0;
                sender.Balance -= Transaction.AllocationFee ?? 0;

                // WTF: [level:349408] - Tezos reset account during transaction.
                if (sender.Balance <= 0 && sender.Type == AccountType.User)
                {
                    sender.Counter = (await Cache.GetAppStateAsync()).ManagerCounter;
                }

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= Transaction.Amount;
                    senderDelegate.StakingBalance -= Transaction.StorageFee ?? 0;
                    senderDelegate.StakingBalance -= Transaction.AllocationFee ?? 0;
                }

                target.Balance += Transaction.Amount;

                if (targetDelegate != null)
                {
                    targetDelegate.StakingBalance += Transaction.Amount;
                }

                // WTF: [level:463226] - Receiving a transaction doesn't update grace period anymore
            }
            #endregion

            Db.TransactionOps.Add(Transaction);
        }

        public Task ApplyInternalTransaction()
        {
            #region entities
            var block = Transaction.Block;

            var parentTx = Transaction.Parent;
            var parentSender = parentTx.Sender;
            var parentDelegate = parentSender.Delegate ?? parentSender as Data.Models.Delegate;

            var sender = Transaction.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var target = Transaction.Target;
            var targetDelegate = target?.Delegate ?? target as Data.Models.Delegate;

            //Db.TryAttach(block);

            //Db.TryAttach(parentTx);
            //Db.TryAttach(parentSender);
            //Db.TryAttach(parentDelegate);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(target);
            Db.TryAttach(targetDelegate);
            #endregion

            #region apply operation
            parentTx.InternalOperations = (parentTx.InternalOperations ?? InternalOperations.None) | InternalOperations.Transactions;

            sender.TransactionsCount++;
            if (target != null) target.TransactionsCount++;

            block.Operations |= Operations.Transactions;
            #endregion

            #region apply result
            if (Transaction.Status == OperationStatus.Applied)
            {
                sender.Balance -= Transaction.Amount;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= Transaction.Amount;
                }

                parentSender.Balance -= Transaction.StorageFee ?? 0;
                parentSender.Balance -= Transaction.AllocationFee ?? 0;

                if (parentDelegate != null)
                {
                    parentDelegate.StakingBalance -= Transaction.StorageFee ?? 0;
                    parentDelegate.StakingBalance -= Transaction.AllocationFee ?? 0;
                }

                target.Balance += Transaction.Amount;

                if (targetDelegate != null)
                {
                    targetDelegate.StakingBalance += Transaction.Amount;
                }
            }
            #endregion

            Db.TransactionOps.Add(Transaction);
            return Task.CompletedTask;
        }

        public async Task RevertTransaction()
        {
            #region entities
            var block = Transaction.Block;
            var blockBaker = block.Baker;

            var sender = Transaction.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var target = Transaction.Target;
            var targetDelegate = target?.Delegate ?? target as Data.Models.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(target);
            Db.TryAttach(targetDelegate);
            #endregion

            #region revert result
            if (Transaction.Status == OperationStatus.Applied)
            {
                sender.Balance += Transaction.Amount;
                sender.Balance += Transaction.StorageFee ?? 0;
                sender.Balance += Transaction.AllocationFee ?? 0;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += Transaction.Amount;
                    senderDelegate.StakingBalance += Transaction.StorageFee ?? 0;
                    senderDelegate.StakingBalance += Transaction.AllocationFee ?? 0;
                }

                target.Balance -= Transaction.Amount;

                if (targetDelegate != null)
                {
                    targetDelegate.StakingBalance -= Transaction.Amount;
                }

                if (target is Data.Models.Delegate delegat)
                {
                    if (Transaction.ResetDeactivation != null)
                    {
                        if (Transaction.ResetDeactivation <= Transaction.Level)
                            await UpdateDelegate(delegat, false);

                        delegat.DeactivationLevel = (int)Transaction.ResetDeactivation;
                    }
                }
            }
            #endregion

            #region revert operation
            sender.Balance += Transaction.BakerFee;
            if (senderDelegate != null) senderDelegate.StakingBalance += Transaction.BakerFee;
            blockBaker.FrozenFees -= Transaction.BakerFee;
            blockBaker.Balance -= Transaction.BakerFee;
            blockBaker.StakingBalance -= Transaction.BakerFee;

            sender.TransactionsCount--;
            if (target != null) target.TransactionsCount--;

            sender.Counter = Math.Min(sender.Counter, Transaction.Counter - 1);
            #endregion

            Db.TransactionOps.Remove(Transaction);
            await Cache.ReleaseCounterAsync(true);
        }

        public async Task RevertInternalTransaction()
        {
            #region entities
            var parentTx = Transaction.Parent;
            var parentSender = parentTx.Sender;
            var parentDelegate = parentSender.Delegate ?? parentSender as Data.Models.Delegate;

            var sender = Transaction.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var target = Transaction.Target;
            var targetDelegate = target?.Delegate ?? target as Data.Models.Delegate;

            //Db.TryAttach(block);

            //Db.TryAttach(parentTx);
            //Db.TryAttach(parentSender);
            //Db.TryAttach(parentDelegate);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(target);
            Db.TryAttach(targetDelegate);
            #endregion

            #region revert result
            if (Transaction.Status == OperationStatus.Applied)
            {
                sender.Balance += Transaction.Amount;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += Transaction.Amount;
                }

                parentSender.Balance += Transaction.StorageFee ?? 0;
                parentSender.Balance += Transaction.AllocationFee ?? 0;

                if (parentDelegate != null)
                {
                    parentDelegate.StakingBalance += Transaction.StorageFee ?? 0;
                    parentDelegate.StakingBalance += Transaction.AllocationFee ?? 0;
                }

                target.Balance -= Transaction.Amount;

                if (targetDelegate != null)
                {
                    targetDelegate.StakingBalance -= Transaction.Amount;
                }

                if (target is Data.Models.Delegate delegat)
                {
                    if (Transaction.ResetDeactivation != null)
                    {
                        if (Transaction.ResetDeactivation <= Transaction.Level)
                            await UpdateDelegate(delegat, false);

                        delegat.DeactivationLevel = (int)Transaction.ResetDeactivation;
                    }
                }
            }
            #endregion

            #region revert operation
            sender.TransactionsCount--;
            if (target != null) target.TransactionsCount--;
            #endregion

            Db.TransactionOps.Remove(Transaction);
        }

        async Task UpdateDelegate(Data.Models.Delegate delegat, bool staked)
        {
            delegat.Staked = staked;

            foreach (var delegator in await Db.Accounts.Where(x => x.DelegateId == delegat.Id).ToListAsync())
            {
                Cache.AddAccount(delegator);
                Db.TryAttach(delegator);

                delegator.Staked = staked;
            }
        }

        #region static
        public static async Task<TransactionsCommit> Apply(ProtocolHandler proto, Block block, RawOperation op, RawTransactionContent content)
        {
            var commit = new TransactionsCommit(proto);
            await commit.Init(block, op, content);
            await commit.Apply();

            return commit;
        }

        public static async Task<TransactionsCommit> Apply(ProtocolHandler proto, Block block, TransactionOperation parent, RawInternalTransactionResult content)
        {
            var commit = new TransactionsCommit(proto);
            await commit.Init(block, parent, content);
            await commit.Apply();

            return commit;
        }

        public static async Task<TransactionsCommit> Revert(ProtocolHandler proto, Block block, TransactionOperation op)
        {
            var commit = new TransactionsCommit(proto);
            await commit.Init(block, op);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
