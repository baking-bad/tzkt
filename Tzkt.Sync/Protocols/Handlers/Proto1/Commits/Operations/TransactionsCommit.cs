using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    class TransactionsCommit : ProtocolCommit
    {
        #region constants
        protected virtual int ByteCost => 1000;
        protected virtual int OriginationCost => 257_000;
        #endregion

        public List<TransactionOperation> Transactions { get; protected set; }

        public TransactionsCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;
            var parsedBlock = FindCommit<BlockCommit>().Block;

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
                            Transactions.Add(await ParseInternalTransaction(transaction, internalContent as RawInternalTransactionResult));
                }
            }
        }

        async Task<TransactionOperation> ParseTransaction(Block block, string opHash, RawTransactionContent content)
        {
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
                Sender = await Accounts.GetAccountAsync(content.Source),
                Target = await Accounts.GetAccountAsync(content.Destination),

                Status = content.Metadata.Result.Status switch
                {
                    "applied" => OperationStatus.Applied,
                    _ => throw new NotImplementedException()
                },
                GasUsed = content.Metadata.Result.ConsumedGas,
                StorageUsed = content.Metadata.Result.PaidStorageSizeDiff,
                StorageFee = content.Metadata.Result.PaidStorageSizeDiff > 0
                    ? (int?)(content.Metadata.Result.PaidStorageSizeDiff * ByteCost)
                    : null,
            };
        }

        async Task<TransactionOperation> ParseInternalTransaction(TransactionOperation parent, RawInternalTransactionResult content)
        {
            return new TransactionOperation
            {
                Parent = parent,

                Block = parent.Block,
                Timestamp = parent.Timestamp,
                OpHash = parent.OpHash,
                Counter = parent.Counter,

                Amount = content.Amount,
                Nonce = content.Nonce,
                Sender = await Accounts.GetAccountAsync(content.Source),
                Target = await Accounts.GetAccountAsync(content.Destination),

                Status = content.Result.Status switch
                {
                    "applied" => OperationStatus.Applied,
                    _ => throw new NotImplementedException()
                },
                GasUsed = content.Result.ConsumedGas,
                StorageUsed = content.Result.PaidStorageSizeDiff,
                StorageFee = content.Result.PaidStorageSizeDiff > 0 ? (int?)(content.Result.PaidStorageSizeDiff * ByteCost) : null,
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

            foreach (var transaction in Transactions)
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
            var block = await State.GetCurrentBlock();
            var blockBaker = block.Baker;

            var sender = await Accounts.GetAccountAsync(transaction.SenderId);
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var target = await Accounts.GetAccountAsync(transaction.TargetId);
            var targetDelegate = target.Delegate ?? target as Data.Models.Delegate;

            Db.TryAttach(block);
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

            if (!await Db.TransactionOps.AnyAsync(x => (x.SenderId == sender.Id || x.TargetId == sender.Id) && x.Counter < transaction.Counter))
                sender.Operations &= ~Operations.Transactions;

            if (!await Db.TransactionOps.AnyAsync(x => (x.SenderId == target.Id || x.TargetId == target.Id) && x.Counter < transaction.Counter))
                target.Operations &= ~Operations.Transactions;

            sender.Counter = Math.Min(sender.Counter, transaction.Counter - 1);
            #endregion

            if (target.Operations == Operations.None && target.Counter > 0)
                Db.Accounts.Remove(target);

            Db.TransactionOps.Remove(transaction);
        }

        public async Task RevertInternalTransaction(TransactionOperation transaction)
        {
            #region entities
            var parentTx = transaction.Parent;
            var parentSender = await Accounts.GetAccountAsync(parentTx.SenderId);
            var parentDelegate = parentSender.Delegate ?? parentSender as Data.Models.Delegate;

            var sender = await Accounts.GetAccountAsync(transaction.SenderId);
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var target = await Accounts.GetAccountAsync(transaction.TargetId);
            var targetDelegate = target.Delegate ?? target as Data.Models.Delegate;

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
            if (!await Db.TransactionOps.AnyAsync(x => (x.SenderId == sender.Id || x.TargetId == sender.Id) && x.Counter < transaction.Counter))
                sender.Operations &= ~Operations.Transactions;

            if (!await Db.TransactionOps.AnyAsync(x => (x.SenderId == target.Id || x.TargetId == target.Id) && x.Counter < transaction.Counter))
                target.Operations &= ~Operations.Transactions;
            #endregion

            if (target.Operations == Operations.None && target.Counter > 0)
                Db.Accounts.Remove(target);

            Db.TransactionOps.Remove(transaction);
        }

        #region static
        public static async Task<TransactionsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new TransactionsCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static Task<TransactionsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, List<TransactionOperation> transactions)
        {
            var commit = new TransactionsCommit(protocol, commits) { Transactions = transactions };
            return Task.FromResult(commit);
        }
        #endregion
    }
}
