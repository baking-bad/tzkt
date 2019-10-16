using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto1
{
    class TransactionsCommit : ProtocolCommit
    {
        public List<TransactionOperation> Transactions { get; private set; }
        public Protocol Protocol { get; private set; }

        public TransactionsCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init()
        {
            var block = await Cache.GetCurrentBlockAsync();
            block.Baker ??= (Data.Models.Delegate)await Cache.GetAccountAsync(block.BakerId);

            Protocol = await Cache.GetCurrentProtocolAsync();
            Transactions = await Db.TransactionOps.Where(x => x.Level == block.Level).ToListAsync();
            foreach (var op in Transactions)
            {
                op.Block = block;
                op.Sender = await Cache.GetAccountAsync(op.SenderId);
                op.Sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(op.Sender.DelegateId);

                op.Target = await Cache.GetAccountAsync(op.TargetId);
                op.Target.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(op.Target.DelegateId);

                op.Parent ??= op.ParentId != null
                    ? Transactions.FirstOrDefault(x => x.Id == op.ParentId)
                    : null;
            }
        }

        public override async Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;
            var parsedBlock = FindCommit<BlockCommit>().Block;
            parsedBlock.Baker ??= (Data.Models.Delegate)await Cache.GetAccountAsync(parsedBlock.BakerId);

            Protocol = await Cache.GetProtocolAsync(block.Protocol);
            Transactions = new List<TransactionOperation>();
            foreach (var op in rawBlock.Operations[3])
            {
                foreach (var content in op.Contents.Where(x => x is RawTransactionContent))
                {
                    var txContent = content as RawTransactionContent;
                    var transaction = await ParseTransaction(parsedBlock, op.Hash, txContent);
                    Transactions.Add(transaction);

                    if (txContent.Metadata.InternalResults?.Count > 0)
                        foreach (var internalContent in txContent.Metadata.InternalResults.Where(x => x is RawInternalTransactionResult))
                            Transactions.Add(await ParseInternalTransaction(transaction, txContent, internalContent as RawInternalTransactionResult));
                }
            }
        }

        async Task<TransactionOperation> ParseTransaction(Block block, string opHash, RawTransactionContent content)
        {
            var sender = await Cache.GetAccountAsync(content.Source);
            sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(sender.DelegateId);

            var target = await Cache.GetAccountAsync(content.Destination);
            target.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(target.DelegateId);

            if ((Db.Entry(target).State == EntityState.Added ||
                target is User && !(target is Data.Models.Delegate) && target.Balance == 0) &&
                target.Counter <= (await Cache.GetAppStateAsync()).Counter)
                target.Counter = content.GlobalCounter;

            return new TransactionOperation
            {
                Block = block, 
                Timestamp = block.Timestamp,

                OpHash = opHash,

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
                    _ => throw new NotImplementedException()
                },
                GasUsed = content.Metadata.Result.ConsumedGas,
                StorageUsed = content.Metadata.Result.PaidStorageSizeDiff,
                StorageFee = content.Metadata.Result.PaidStorageSizeDiff > 0
                    ? (int?)(content.Metadata.Result.PaidStorageSizeDiff * Protocol.ByteCost)
                    : null,
            };
        }

        async Task<TransactionOperation> ParseInternalTransaction(TransactionOperation parent, RawTransactionContent parentContent, RawInternalTransactionResult content)
        {
            var sender = await Cache.GetAccountAsync(content.Source);
            sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(sender.DelegateId);

            var target = await Cache.GetAccountAsync(content.Destination);
            target.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(target.DelegateId);

            if ((Db.Entry(target).State == EntityState.Added ||
                target is User && !(target is Data.Models.Delegate) && target.Balance == 0) &&
                target.Counter <= (await Cache.GetAppStateAsync()).Counter)
                target.Counter = parentContent.GlobalCounter;

            return new TransactionOperation
            {
                Parent = parent,

                Block = parent.Block,
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
                    _ => throw new NotImplementedException()
                },
                GasUsed = content.Result.ConsumedGas,
                StorageUsed = content.Result.PaidStorageSizeDiff,
                StorageFee = content.Result.PaidStorageSizeDiff > 0 ? (int?)(content.Result.PaidStorageSizeDiff * Protocol.ByteCost) : null,
            };
        }

        public override async Task Apply()
        {
            if (Transactions == null)
                throw new Exception("Commit is not initialized");

            foreach (var transaction in Transactions)
            {
                if (transaction.Parent == null)
                    await ApplyTransaction(transaction);
                else
                    await ApplyInternalTransaction(transaction);
            }
        }

        public Task ApplyTransaction(TransactionOperation transaction)
        {
            #region entities
            var block = transaction.Block;
            var blockBaker = block.Baker;

            var sender = transaction.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var target = transaction.Target;
            var targetDelegate = target.Delegate ?? target as Data.Models.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(target);
            Db.TryAttach(targetDelegate);
            #endregion

            #region apply operation
            sender.Balance -= transaction.BakerFee;
            if (senderDelegate != null) senderDelegate.StakingBalance -= transaction.BakerFee;
            blockBaker.FrozenFees += transaction.BakerFee;
            blockBaker.Balance += transaction.BakerFee;
            blockBaker.StakingBalance += transaction.BakerFee;

            sender.Operations |= Operations.Transactions;
            target.Operations |= Operations.Transactions;
            block.Operations |= Operations.Transactions;

            sender.Counter = Math.Max(sender.Counter, transaction.Counter);
            #endregion

            #region apply result
            if (transaction.Status == OperationStatus.Applied)
            {
                sender.Balance -= transaction.Amount;
                sender.Balance -= transaction.StorageFee ?? 0;
                sender.Balance -= transaction.AllocationFee ?? 0;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= transaction.Amount;
                    senderDelegate.StakingBalance -= transaction.StorageFee ?? 0;
                    senderDelegate.StakingBalance -= transaction.AllocationFee ?? 0;
                }

                target.Balance += transaction.Amount;

                if (targetDelegate != null)
                {
                    targetDelegate.StakingBalance += transaction.Amount;
                }
            }
            #endregion

            Db.TransactionOps.Add(transaction);
            return Task.CompletedTask;
        }

        public Task ApplyInternalTransaction(TransactionOperation transaction)
        {
            #region entities
            var block = transaction.Block;

            var parentTx = transaction.Parent;
            var parentSender = parentTx.Sender;
            var parentDelegate = parentSender.Delegate ?? parentSender as Data.Models.Delegate;

            var sender = transaction.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var target = transaction.Target;
            var targetDelegate = target.Delegate ?? target as Data.Models.Delegate;

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
            sender.Operations |= Operations.Transactions;
            target.Operations |= Operations.Transactions;
            block.Operations |= Operations.Transactions;
            #endregion

            #region apply result
            if (transaction.Status == OperationStatus.Applied)
            {
                sender.Balance -= transaction.Amount;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= transaction.Amount;
                }

                parentSender.Balance -= transaction.StorageFee ?? 0;
                parentSender.Balance -= transaction.AllocationFee ?? 0;

                if (parentDelegate != null)
                {
                    parentDelegate.StakingBalance -= transaction.StorageFee ?? 0;
                    parentDelegate.StakingBalance -= transaction.AllocationFee ?? 0;
                }

                target.Balance += transaction.Amount;

                if (targetDelegate != null)
                {
                    targetDelegate.StakingBalance += transaction.Amount;
                }

                parentTx.GasUsed += transaction.GasUsed;
                parentTx.StorageUsed += transaction.StorageUsed;

                if (transaction.AllocationFee != null)
                    parentTx.AllocationFee = (parentTx.AllocationFee ?? 0) + transaction.AllocationFee;

                if (transaction.StorageFee != null)
                    parentTx.StorageFee = (parentTx.StorageFee ?? 0) + transaction.StorageFee;
            }
            #endregion

            Db.TransactionOps.Add(transaction);
            return Task.CompletedTask;
        }

        public override async Task Revert()
        {
            if (Transactions == null)
                throw new Exception("Commit is not initialized");

            foreach (var transaction in Transactions
                .OrderBy(x => x.Parent?.SenderId ?? x.SenderId)
                .ThenByDescending(x => x.Counter)
                .ThenByDescending(x => x.ParentId ?? 0))
            {
                if (transaction.ParentId == null)
                    await RevertTransaction(transaction);
                else
                    await RevertInternalTransaction(transaction);
            }
        }

        public async Task RevertTransaction(TransactionOperation transaction)
        {
            #region entities
            var block = transaction.Block;
            var blockBaker = block.Baker;

            var sender = transaction.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var target = transaction.Target;
            var targetDelegate = target.Delegate ?? target as Data.Models.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(target);
            Db.TryAttach(targetDelegate);
            #endregion

            #region revert result
            if (transaction.Status == OperationStatus.Applied)
            {
                sender.Balance += transaction.Amount;
                sender.Balance += transaction.StorageFee ?? 0;
                sender.Balance += transaction.AllocationFee ?? 0;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += transaction.Amount;
                    senderDelegate.StakingBalance += transaction.StorageFee ?? 0;
                    senderDelegate.StakingBalance += transaction.AllocationFee ?? 0;
                }

                target.Balance -= transaction.Amount;

                if (targetDelegate != null)
                {
                    targetDelegate.StakingBalance -= transaction.Amount;
                }
            }
            #endregion

            #region revert operation
            sender.Balance += transaction.BakerFee;
            if (senderDelegate != null) senderDelegate.StakingBalance += transaction.BakerFee;
            blockBaker.FrozenFees -= transaction.BakerFee;
            blockBaker.Balance -= transaction.BakerFee;
            blockBaker.StakingBalance -= transaction.BakerFee;

            if (!await Db.TransactionOps.AnyAsync(x => (x.SenderId == sender.Id || x.TargetId == sender.Id) && x.Level < transaction.Level))
                sender.Operations &= ~Operations.Transactions;

            if (!await Db.TransactionOps.AnyAsync(x => (x.SenderId == target.Id || x.TargetId == target.Id) && x.Level < transaction.Level))
                target.Operations &= ~Operations.Transactions;

            sender.Counter = Math.Min(sender.Counter, transaction.Counter - 1);
            #endregion

            if (target.Operations == Operations.None && target.Counter > 0)
                Db.Accounts.Remove(target);

            var list = Db.ChangeTracker.Entries().ToList();

            Db.TransactionOps.Remove(transaction);
        }

        public async Task RevertInternalTransaction(TransactionOperation transaction)
        {
            #region entities
            var parentTx = transaction.Parent;
            var parentSender = parentTx.Sender;
            var parentDelegate = parentSender.Delegate ?? parentSender as Data.Models.Delegate;

            var sender = transaction.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var target = transaction.Target;
            var targetDelegate = target.Delegate ?? target as Data.Models.Delegate;

            //Db.TryAttach(block);

            //Db.TryAttach(parentTx);
            //Db.TryAttach(parentSender);
            //Db.TryAttach(parentDelegate);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(target);
            Db.TryAttach(targetDelegate);
            #endregion

            #region apply result
            if (transaction.Status == OperationStatus.Applied)
            {
                sender.Balance += transaction.Amount;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += transaction.Amount;
                }

                parentSender.Balance += transaction.StorageFee ?? 0;
                parentSender.Balance += transaction.AllocationFee ?? 0;

                if (parentDelegate != null)
                {
                    parentDelegate.StakingBalance += transaction.StorageFee ?? 0;
                    parentDelegate.StakingBalance += transaction.AllocationFee ?? 0;
                }

                target.Balance -= transaction.Amount;

                if (targetDelegate != null)
                {
                    targetDelegate.StakingBalance -= transaction.Amount;
                }
            }
            #endregion

            #region revert operation
            if (!await Db.TransactionOps.AnyAsync(x => (x.SenderId == sender.Id || x.TargetId == sender.Id) && x.Level < transaction.Level))
                sender.Operations &= ~Operations.Transactions;

            if (!await Db.TransactionOps.AnyAsync(x => (x.SenderId == target.Id || x.TargetId == target.Id) && x.Level < transaction.Level))
                target.Operations &= ~Operations.Transactions;
            #endregion

            if (target.Operations == Operations.None && target.Counter > 0)
            {
                Db.Accounts.Remove(target);
                Cache.RemoveAccount(target);
            }

            Db.TransactionOps.Remove(transaction);
        }

        #region static
        public static async Task<TransactionsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new TransactionsCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static async Task<TransactionsCommit> Create(ProtocolHandler protocol, List<ICommit> commits)
        {
            var commit = new TransactionsCommit(protocol, commits);
            await commit.Init();
            return commit;
        }
        #endregion
    }
}
