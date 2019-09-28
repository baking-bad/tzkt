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
    public class TransactionsCommit : ICommit<List<TransactionOperation>>
    {
        #region constants
        protected virtual int ByteCost => 1000;
        protected virtual int OriginationCost => 257_000;
        #endregion

        public List<TransactionOperation> Content { get; protected set; }

        protected readonly TzktContext Db;
        protected readonly AccountsCache Accounts;
        protected readonly ProtocolsCache Protocols;
        protected readonly StateCache State;

        public TransactionsCommit(TzktContext db, CacheService cache)
        {
            Db = db;
            Accounts = cache.Accounts;
            Protocols = cache.Protocols;
            State = cache.State;
        }

        public virtual async Task<TransactionsCommit> Init(JToken rawBlock, Block parsedBlock)
        {
            await Validate(rawBlock);
            Content = await Parse(rawBlock, parsedBlock);
            return this;
        }

        public virtual Task<TransactionsCommit> Init(List<TransactionOperation> operations)
        {
            Content = operations;
            return Task.FromResult(this);
        }

        public async Task Apply()
        {
            foreach (var transaction in Content)
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

        public async Task Revert()
        {
            foreach (var transaction in Content)
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

            Db.Delegates.Update(baker);
            Db.Accounts.Update(sender);
            Db.Accounts.Update(target);
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

            Db.Accounts.Update(sender);
            Db.Accounts.Update(target);
            Db.Accounts.Update(initiator);
            Db.TransactionOps.Remove(transaction);
        }

        public async Task Validate(JToken block)
        {
            foreach (var operation in block["operations"]?[3] ?? throw new Exception("Manager operations missed"))
            {
                operation.RequireValue("hash");
                operation.RequireArray("contents");

                foreach (var content in operation["contents"]
                    .Where(x => (x["kind"]?.String() ?? throw new Exception("Invalid content kind")) == "transaction"))
                {
                    content.RequireValue("source");
                    content.RequireValue("fee");
                    content.RequireValue("counter");
                    content.RequireValue("gas_limit");
                    content.RequireValue("storage_limit");
                    content.RequireValue("amount");
                    content.RequireValue("destination");
                    content.RequireObject("metadata");

                    var src = content["source"].String();
                    if (!await Accounts.ExistsAsync(src, AccountType.User))
                        throw new Exception("Unknown source account");

                    var dst = content["destination"].String();

                    var metadata = content["metadata"];
                    metadata.RequireArray("balance_updates");
                    metadata.RequireObject("operation_result");

                    var fee = content["fee"].Int64();
                    var opUpdates = BalanceUpdates.Parse((JArray)metadata["balance_updates"]);
                    if ((fee == 0 && opUpdates.Count != 0) || (fee != 0 && opUpdates.Count != 2))
                        throw new Exception($"Invalid transaction balance updates count");

                    if (opUpdates.Count > 0)
                    {
                        var senderFeeUpdate = opUpdates.FirstOrDefault(x => x is ContractUpdate) as ContractUpdate;
                        var bakerFeeUpdate = opUpdates.FirstOrDefault(x => x is FeesUpdate) as FeesUpdate;

                        if (senderFeeUpdate == null ||
                            bakerFeeUpdate == null ||
                            senderFeeUpdate.Change != -bakerFeeUpdate.Change ||
                            bakerFeeUpdate.Change != fee || 
                            senderFeeUpdate.Contract != src ||
                            bakerFeeUpdate.Delegate != block["metadata"]["baker"].String() ||
                            bakerFeeUpdate.Level != block["metadata"]["level"]["cycle"].Int32())
                            throw new Exception($"Invalid transaction fee balance updates");
                    }

                    var result = metadata["operation_result"];
                    result.RequireValue("status");
                    result.RequireValue("consumed_gas");

                    if (result["status"].String() != "applied")
                        throw new NotSupportedException();

                    var amount = content["amount"].Int64();
                    var allocated = result["allocated_destination_contract"]?.Bool() ?? false;
                    var paidStorage = result["paid_storage_size_diff"]?.Int64() ?? 0;

                    if (amount > 0 || paidStorage > 0 || allocated)
                    {
                        result.RequireArray("balance_updates");

                        var estUpdates = (amount != 0 ? 2 : 0) + (allocated ? 1 : 0) + (paidStorage != 0 ? 1 : 0);
                        var resultUpdates = BalanceUpdates.Parse((JArray)result["balance_updates"]);

                        if (resultUpdates.Count != estUpdates)
                            throw new Exception($"Invalid transaction result balance updates count");

                        if (resultUpdates.Count > 0)
                        {
                            if (amount != 0 &&
                                (resultUpdates.FirstOrDefault(x => x is ContractUpdate && x.Change == -amount && (x as ContractUpdate).Contract == src) == null ||
                                resultUpdates.FirstOrDefault(x => x is ContractUpdate && x.Change == amount && (x as ContractUpdate).Contract == dst) == null))
                                throw new Exception($"Invalid transaction amount balance updates");

                            if (allocated &&
                                resultUpdates.FirstOrDefault(x => x is ContractUpdate && x.Change == OriginationCost && (x as ContractUpdate).Contract == src) == null)
                                throw new Exception($"Invalid transaction allocation balance updates");

                            if (paidStorage != 0 &&
                                resultUpdates.FirstOrDefault(x => x is ContractUpdate && x.Change == paidStorage * ByteCost && (x as ContractUpdate).Contract == src) == null)
                                throw new Exception($"Invalid transaction storage balance updates");
                        }
                    }

                    if (metadata["internal_operation_results"] != null)
                        foreach (var internalContent in metadata["internal_operation_results"]
                            .Where(x => (x["kind"]?.String() ?? throw new Exception("Invalid internal content kind")) == "transaction"))
                        {
                            internalContent.RequireValue("source");
                            internalContent.RequireValue("nonce");
                            internalContent.RequireValue("amount");
                            internalContent.RequireValue("destination");
                            internalContent.RequireObject("result");

                            var internalSrc = internalContent["source"].String();
                            if (!await Accounts.ExistsAsync(internalSrc, AccountType.Contract))
                                throw new Exception("Unknown source contract");

                            var internalDst = internalContent["destination"].String();

                            var internalResult = internalContent["result"];
                            internalResult.RequireValue("status");
                            internalResult.RequireArray("balance_updates");
                            internalResult.RequireValue("consumed_gas");

                            if (internalResult["status"].String() != "applied")
                                throw new NotSupportedException();

                            var internalAmount = internalContent["amount"].Int64();
                            var internalAllocated = internalResult["allocated_destination_contract"]?.Bool() ?? false;
                            var internalPaidStorage = internalResult["paid_storage_size_diff"]?.Int64() ?? 0;

                            var internalEstUpdates = (internalAmount != 0 ? 2 : 0) + (internalAllocated ? 1 : 0) + (internalPaidStorage != 0 ? 1 : 0);
                            var internalResultUpdates = BalanceUpdates.Parse((JArray)internalResult["balance_updates"]);

                            if (internalResultUpdates.Count != internalEstUpdates)
                                throw new Exception($"Invalid transaction result balance updates count");

                            if (internalResultUpdates.Count > 0)
                            {
                                if (internalAmount != 0 &&
                                    (internalResultUpdates.FirstOrDefault(x => x is ContractUpdate && x.Change == -internalAmount && (x as ContractUpdate).Contract == internalSrc) == null ||
                                    internalResultUpdates.FirstOrDefault(x => x is ContractUpdate && x.Change == internalAmount && (x as ContractUpdate).Contract == internalDst) == null))
                                    throw new Exception($"Invalid transaction amount balance updates");

                                if (internalAllocated &&
                                    internalResultUpdates.FirstOrDefault(x => x is ContractUpdate && x.Change == OriginationCost && (x as ContractUpdate).Contract == internalSrc) == null)
                                    throw new Exception($"Invalid transaction allocation balance updates");

                                if (internalPaidStorage != 0 &&
                                    internalResultUpdates.FirstOrDefault(x => x is ContractUpdate && x.Change == internalPaidStorage * ByteCost && (x as ContractUpdate).Contract == internalSrc) == null)
                                    throw new Exception($"Invalid transaction storage balance updates");
                            }
                        }
                }
            }
        }

        public async Task<List<TransactionOperation>> Parse(JToken rawBlock, Block parsedBlock)
        {
            var result = new List<TransactionOperation>();

            foreach (var operation in rawBlock["operations"][3])
            {
                var opHash = operation["hash"].String();

                foreach (var content in operation["contents"].Where(x => x["kind"].String() == "transaction"))
                {
                    var transaction = await ParseTransaction(parsedBlock, opHash, content);
                    result.Add(transaction);

                    if (content["metadata"]["internal_operation_results"] != null)
                        foreach (var internalContent in content["metadata"]["internal_operation_results"].Where(x => x["kind"].String() == "transaction"))
                            result.Add(await ParseInternalTransaction(transaction, internalContent));
                }
            }

            return result;
        }

        async Task<TransactionOperation> ParseTransaction(Block block, string opHash, JToken content)
        {
            var metadata = content["metadata"];
            var result = metadata["operation_result"];
            var storageUsed = result["paid_storage_size_diff"]?.Int32() ?? 0;
            var targetAllocated = result["allocated_destination_contract"]?.Bool() == true;

            return new TransactionOperation
            {
                Block = block,
                Timestamp = block.Timestamp,

                OpHash = opHash,

                Amount = content["amount"].Int64(),
                BakerFee = content["fee"].Int64(),
                Counter = content["counter"].Int32(),
                GasLimit = content["gas_limit"].Int32(),
                StorageLimit = content["storage_limit"].Int32(),
                Sender = await Accounts.GetAccountAsync(content["source"].String()),
                Target = await Accounts.GetAccountAsync(content["destination"].String()),

                Status = result["status"].OperationStatus(),
                GasUsed = result["consumed_gas"].Int32(),
                StorageUsed = storageUsed,
                StorageFee = storageUsed > 0 ? (int?)(storageUsed * ByteCost) : null,
                AllocationFee = targetAllocated ? (int?)OriginationCost : null
            };
        }

        async Task<TransactionOperation> ParseInternalTransaction(TransactionOperation parent, JToken content)
        {
            var result = content["result"];
            var storageUsed = result["paid_storage_size_diff"]?.Int32() ?? 0;
            var targetAllocated = result["allocated_destination_contract"]?.Bool() == true;

            return new TransactionOperation
            {
                Parent = parent,

                Block = parent.Block,
                Timestamp = parent.Timestamp,
                OpHash = parent.OpHash,
                Counter = parent.Counter,

                Amount = content["amount"].Int64(),
                Nonce = content["nonce"].Int32(),
                Sender = await Accounts.GetAccountAsync(content["source"].String()),
                Target = await Accounts.GetAccountAsync(content["destination"].String()),

                Status = result["status"].OperationStatus(),
                GasUsed = result["consumed_gas"].Int32(),
                StorageUsed = storageUsed,
                StorageFee = storageUsed > 0 ? (int?)(storageUsed * ByteCost) : null,
                AllocationFee = targetAllocated ? (int?)OriginationCost : null
            };
        }
    }
}
