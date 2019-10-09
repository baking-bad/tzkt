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
            #region balances
            var baker = transaction.Block.Baker;
            var sender = transaction.Sender;
            var target = transaction.Target;

            baker.FrozenFees += transaction.BakerFee;
            sender.Balance -= transaction.BakerFee;

            sender.Balance -= transaction.Amount;
            target.Balance += transaction.Amount;

            sender.Balance -= transaction.StorageFee ?? 0;
            sender.Balance -= transaction.AllocationFee ?? 0;
            #endregion

            #region counters
            sender.Operations |= Operations.Transactions;
            target.Operations |= Operations.Transactions;

            sender.Counter = Math.Max(sender.Counter, transaction.Counter);

            transaction.Block.Operations |= Operations.Transactions;
            #endregion

            if (Db.Entry(baker).State != EntityState.Added)
                Db.Delegates.Update(baker);

            if (Db.Entry(sender).State != EntityState.Added)
                Db.Accounts.Update(sender);

            if (Db.Entry(target).State != EntityState.Added)
                Db.Accounts.Update(target);

            Db.TransactionOps.Add(transaction);

            return Task.CompletedTask;
        }

        public Task ApplyInternalTransaction(TransactionOperation transaction)
        {
            #region balances
            var sender = transaction.Sender;
            var target = transaction.Target;
            var parent = transaction.Parent;
            var initiator = parent.Sender;

            sender.Balance -= transaction.Amount;
            target.Balance += transaction.Amount;

            initiator.Balance -= transaction.StorageFee ?? 0;
            initiator.Balance -= transaction.AllocationFee ?? 0;
            #endregion

            #region counters
            sender.Operations |= Operations.Transactions;
            target.Operations |= Operations.Transactions;
            parent.InternalOperations = (parent.InternalOperations ?? InternalOperations.None) | InternalOperations.Transactions;

            transaction.Block.Operations |= Operations.Transactions;
            #endregion

            parent.GasUsed += transaction.GasUsed;
            parent.StorageUsed += transaction.StorageUsed;

            if (transaction.AllocationFee != null)
                parent.AllocationFee = (parent.AllocationFee ?? 0) + transaction.AllocationFee;

            if (transaction.StorageFee != null)
                parent.StorageFee = (parent.StorageFee ?? 0) + transaction.StorageFee;

            if (Db.Entry(sender).State != EntityState.Added)
                Db.Accounts.Update(sender);

            if (Db.Entry(target).State != EntityState.Added)
                Db.Accounts.Update(target);

            if (Db.Entry(initiator).State != EntityState.Added)
                Db.Accounts.Update(initiator);

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
            #region balances
            var block = await State.GetCurrentBlock();

            var baker = (Data.Models.Delegate)await Accounts.GetAccountAsync(block.BakerId.Value);
            var sender = await Accounts.GetAccountAsync(transaction.SenderId);
            var target = await Accounts.GetAccountAsync(transaction.TargetId);

            baker.FrozenFees -= transaction.BakerFee;
            sender.Balance += transaction.BakerFee;

            sender.Balance += transaction.Amount;
            target.Balance -= transaction.Amount;

            sender.Balance += transaction.StorageFee ?? 0;
            sender.Balance += transaction.AllocationFee ?? 0;
            #endregion

            #region counters
            if (!await Db.TransactionOps.AnyAsync(x => (x.Sender.Id == sender.Id || x.TargetId == sender.Id) && x.Id != transaction.Id))
                sender.Operations &= ~Operations.Transactions;

            if (!await Db.TransactionOps.AnyAsync(x => (x.Sender.Id == target.Id || x.TargetId == target.Id) && x.Id != transaction.Id))
                target.Operations &= ~Operations.Transactions;

            sender.Counter = Math.Min(sender.Counter, transaction.Counter - 1);
            #endregion

            if (target.Operations == Operations.None && target.Counter > 0)
                Db.Accounts.Remove(target);
            else
                Db.Accounts.Update(target);

            Db.Delegates.Update(baker);
            Db.Accounts.Update(sender);
            Db.TransactionOps.Remove(transaction);
        }

        public async Task RevertInternalTransaction(TransactionOperation transaction)
        {
            #region balances
            var parent = transaction.Parent;
            var sender = await Accounts.GetAccountAsync(transaction.SenderId);
            var target = await Accounts.GetAccountAsync(transaction.TargetId);
            var initiator = await Accounts.GetAccountAsync(parent.SenderId);

            sender.Balance += transaction.Amount;
            target.Balance -= transaction.Amount;

            initiator.Balance += transaction.StorageFee ?? 0;
            initiator.Balance += transaction.AllocationFee ?? 0;
            #endregion

            #region counters
            if (!await Db.TransactionOps.AnyAsync(x => (x.Sender.Id == sender.Id || x.TargetId == sender.Id) && x.Id != transaction.Id))
                sender.Operations &= ~Operations.Transactions;

            if (!await Db.TransactionOps.AnyAsync(x => (x.Sender.Id == target.Id || x.TargetId == target.Id) && x.Id != transaction.Id))
                target.Operations &= ~Operations.Transactions;
            #endregion

            if (target.Operations == Operations.None && target.Counter > 0)
                Db.Accounts.Remove(target);
            else
                Db.Accounts.Update(target);

            Db.Accounts.Update(sender);
            Db.Accounts.Update(initiator);
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
