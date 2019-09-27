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

        public Task Apply()
        {
            foreach (var transaction in Content)
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

                Db.Delegates.Update(baker);
                Db.Accounts.Update(sender);
                Db.Accounts.Update(target);
                Db.TransactionOps.Add(transaction);
            }

            return Task.CompletedTask;
        }

        public async Task Revert()
        {
            foreach (var transaction in Content)
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
        }

        public async Task Validate(JToken block)
        {
            foreach (var operation in block["operations"]?[3] ?? throw new Exception("Manager operations missed"))
            {
                var opHash = operation["hash"]?.String();
                if (String.IsNullOrEmpty(opHash))
                    throw new Exception($"Invalid manager operation hash '{opHash}'");

                foreach (var content in operation["contents"]
                    .Where(x => (x["kind"]?.String() ?? throw new Exception("Invalid content kind")) == "transaction"))
                {
                    var metadata = content["metadata"];

                    if (metadata["internal_operation_results"] != null)
                        throw new NotImplementedException();

                    if (!await Accounts.ExistsAsync(content["source"].String(), AccountType.User))
                        throw new Exception("Unknown source");
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
                    var metadata = content["metadata"];
                    var storageUsed = metadata["operation_result"]["paid_storage_size_diff"]?.Int32() ?? 0;
                    var targetAllocated = metadata["operation_result"]["allocated_destination_contract"]?.Bool() == true;

                    result.Add(new TransactionOperation
                    {
                        OpHash = opHash,
                        Block = parsedBlock,
                        Timestamp = parsedBlock.Timestamp,
                        Amount = content["amount"].Int64(),
                        BakerFee = content["fee"].Int64(),
                        Counter = content["counter"].Int32(),
                        GasLimit = content["gas_limit"].Int32(),
                        GasUsed = metadata["operation_result"]["consumed_gas"].Int32(),
                        StorageLimit = content["storage_limit"].Int32(),
                        StorageUsed = storageUsed,
                        StorageFee = storageUsed * ByteCost,
                        Status = ParseStatus(metadata["operation_result"]["status"].String()),
                        Sender = await Accounts.GetAccountAsync(content["source"].String()),
                        Target = await Accounts.GetAccountAsync(content["destination"].String()),
                        TargetAllocated = targetAllocated,
                        AllocationFee = targetAllocated ? OriginationCost : 0
                    });
                }
            }

            return result;
        }

        OperationStatus ParseStatus(string status) => status switch
        {
            "applied" => OperationStatus.Applied,
            _ => throw new NotImplementedException()
        };
    }
}
