using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Netezos.Contracts;
using Netezos.Encoding;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto1
{
    class TransactionsCommit : ProtocolCommit
    {
        public TransactionOperation Transaction { get; private set; }
        public IEnumerable<BigMapDiff> BigMapDiffs { get; private set; }

        public TransactionsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);

            var target = await Cache.Accounts.GetAsync(content.OptionalString("destination"))
                ?? block.Originations?.FirstOrDefault(x => x.Contract.Address == content.OptionalString("destination"))?.Contract;

            if (target != null)
                target.Delegate ??= Cache.Accounts.GetDelegate(target.DelegateId);

            var result = content.Required("metadata").Required("operation_result");

            var transaction = new TransactionOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                Amount = content.RequiredInt64("amount"),
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                Sender = sender,
                Target = target,
                TargetCodeHash = (target as Contract)?.CodeHash,
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
                GasUsed = GetConsumedGas(result),
                StorageUsed = result.OptionalInt32("paid_storage_size_diff") ?? 0,
                StorageFee = result.OptionalInt32("paid_storage_size_diff") > 0
                    ? result.OptionalInt32("paid_storage_size_diff") * block.Protocol.ByteCost
                    : null,
                AllocationFee = HasAllocated(result)
                    ? (long?)block.Protocol.OriginationSize * block.Protocol.ByteCost
                    : null
            };


            if (transaction.Target is not User && content.TryGetProperty("parameters", out var parameters))
                await ProcessParameters(transaction, parameters);
            #endregion

            #region entities
            //var block = transaction.Block;
            var blockBaker = block.Proposer;

            //var sender = transaction.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            //var target = transaction.Target;
            var targetDelegate = target?.Delegate ?? target as Data.Models.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(target);
            Db.TryAttach(targetDelegate);
            #endregion

            #region apply operation
            sender.Balance -= transaction.BakerFee;
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance -= transaction.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance -= transaction.BakerFee;
            }
            blockBaker.Balance += transaction.BakerFee;
            blockBaker.StakingBalance += transaction.BakerFee;

            sender.TransactionsCount++;
            if (target != null && target != sender) target.TransactionsCount++;

            block.Events |= GetBlockEvents(target);
            block.Operations |= Operations.Transactions;
            block.Fees += transaction.BakerFee;

            sender.Counter = transaction.Counter;
            #endregion

            #region apply result
            if (transaction.Status == OperationStatus.Applied)
            {
                var burned = (transaction.StorageFee ?? 0) + (transaction.AllocationFee ?? 0);
                var spent = transaction.Amount + burned;
                Proto.Manager.Burn(burned);

                sender.Balance -= spent;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= spent;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance -= spent;
                }

                target.Balance += transaction.Amount;
                if (targetDelegate != null)
                {
                    targetDelegate.StakingBalance += transaction.Amount;
                    if (targetDelegate.Id != target.Id)
                        targetDelegate.DelegatedBalance += transaction.Amount;
                }

                await ResetGracePeriod(transaction);

                if (result.TryGetProperty("storage", out var storage))
                {
                    BigMapDiffs = ParseBigMapDiffs(transaction, result);
                    await ProcessStorage(transaction, storage);
                }
            }
            #endregion

            Proto.Manager.Set(transaction.Sender);
            Db.TransactionOps.Add(transaction);
            Transaction = transaction;
        }

        public virtual async Task ApplyInternal(Block block, ManagerOperation parent, JsonElement content)
        {
            #region init
            var id = Cache.AppState.NextOperationId();

            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"))
                ?? block.Originations?.FirstOrDefault(x => x.Contract.Address == content.RequiredString("source"))?.Contract
                    ?? throw new ValidationException("Transaction source address doesn't exist");

            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);

            var target = await Cache.Accounts.GetAsync(content.OptionalString("destination"))
                ?? block.Originations?.FirstOrDefault(x => x.Contract.Address == content.OptionalString("destination"))?.Contract;

            if (target != null)
                target.Delegate ??= Cache.Accounts.GetDelegate(target.DelegateId);

            var result = content.Required("result");

            var transaction = new TransactionOperation
            {
                Id = id,
                Initiator = parent.Sender,
                Block = parent.Block,
                Level = parent.Block.Level,
                Timestamp = parent.Timestamp,
                OpHash = parent.OpHash,
                Counter = parent.Counter,
                Amount = content.RequiredInt64("amount"),
                Nonce = content.RequiredInt32("nonce"),
                Sender = sender,
                SenderCodeHash = (sender as Contract)?.CodeHash,
                Target = target,
                TargetCodeHash = (target as Contract)?.CodeHash,
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
                GasUsed = GetConsumedGas(result),
                StorageUsed = result.OptionalInt32("paid_storage_size_diff") ?? 0,
                StorageFee = result.OptionalInt32("paid_storage_size_diff") > 0
                    ? result.OptionalInt32("paid_storage_size_diff") * block.Protocol.ByteCost
                    : null,
                AllocationFee = HasAllocated(result)
                    ? (long?)block.Protocol.OriginationSize * block.Protocol.ByteCost
                    : null
            };

            if (transaction.Target is not User && content.TryGetProperty("parameters", out var parameters))
                await ProcessParameters(transaction, parameters);
            #endregion

            #region entities
            //var block = transaction.Block;
            var parentSender = parent.Sender;
            var parentDelegate = parentSender.Delegate ?? parentSender as Data.Models.Delegate;
            //var sender = transaction.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            //var target = transaction.Target;
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
            if (parent is TransactionOperation parentTx)
            {
                parentTx.InternalOperations = (short?)((parentTx.InternalOperations ?? 0) + 1);
                parentTx.InternalTransactions = (short?)((parentTx.InternalTransactions ?? 0) + 1);
            }

            sender.TransactionsCount++;
            if (target != null && target != sender) target.TransactionsCount++;
            if (parentSender != sender && parentSender != target) parentSender.TransactionsCount++;

            block.Events |= GetBlockEvents(target);
            block.Operations |= Operations.Transactions;
            #endregion

            #region apply result
            if (transaction.Status == OperationStatus.Applied)
            {
                var burned = (transaction.StorageFee ?? 0) + (transaction.AllocationFee ?? 0);
                Proto.Manager.Burn(burned);

                parentSender.Balance -= burned;
                if (parentDelegate != null)
                {
                    parentDelegate.StakingBalance -= burned;
                    if (parentDelegate.Id != parentSender.Id)
                        parentDelegate.DelegatedBalance -= burned;
                }

                sender.Balance -= transaction.Amount;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= transaction.Amount;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance -= transaction.Amount;
                }

                target.Balance += transaction.Amount;
                if (target.Id == parentSender.Id)
                    Proto.Manager.Credit(transaction.Amount);

                if (targetDelegate != null)
                {
                    targetDelegate.StakingBalance += transaction.Amount;
                    if (targetDelegate.Id != target.Id)
                        targetDelegate.DelegatedBalance += transaction.Amount;
                }

                await ResetGracePeriod(transaction);

                if (result.TryGetProperty("storage", out var storage))
                {
                    BigMapDiffs = ParseBigMapDiffs(transaction, result);
                    await ProcessStorage(transaction, storage);
                }
            }
            #endregion

            Db.TransactionOps.Add(transaction);
            Transaction = transaction;
        }

        public virtual async Task Revert(Block block, TransactionOperation transaction)
        {
            #region init
            transaction.Block ??= block;
            transaction.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            transaction.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            transaction.Sender = await Cache.Accounts.GetAsync(transaction.SenderId);
            transaction.Sender.Delegate ??= Cache.Accounts.GetDelegate(transaction.Sender.DelegateId);
            transaction.Target = await Cache.Accounts.GetAsync(transaction.TargetId);

            if (transaction.Target != null)
                transaction.Target.Delegate ??= Cache.Accounts.GetDelegate(transaction.Target.DelegateId);

            if (transaction.InitiatorId != null)
            {
                transaction.Initiator = await Cache.Accounts.GetAsync(transaction.InitiatorId);
                transaction.Initiator.Delegate ??= Cache.Accounts.GetDelegate(transaction.Initiator.DelegateId);
            }
            #endregion

            #region entities
            //var block = transaction.Block;
            var blockBaker = block.Proposer;
            var sender = transaction.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            var target = transaction.Target;
            var targetDelegate = target?.Delegate ?? target as Data.Models.Delegate;

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
                target.Balance -= transaction.Amount;

                if (targetDelegate != null)
                {
                    targetDelegate.StakingBalance -= transaction.Amount;
                    if (targetDelegate.Id != target.Id)
                        targetDelegate.DelegatedBalance -= transaction.Amount;
                }

                if (target is Data.Models.Delegate delegat)
                {
                    if (transaction.ResetDeactivation != null)
                    {
                        if (transaction.ResetDeactivation <= transaction.Level)
                            await UpdateDelegate(delegat, false);

                        delegat.DeactivationLevel = (int)transaction.ResetDeactivation;
                    }
                }

                var spent = transaction.Amount + (transaction.StorageFee ?? 0) + (transaction.AllocationFee ?? 0);

                sender.Balance += spent;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += spent;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance += spent;
                }

                if (transaction.StorageId != null)
                    await RevertStorage(transaction);
            }
            #endregion

            #region revert operation
            sender.Balance += transaction.BakerFee;
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance += transaction.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance += transaction.BakerFee;
            }
            blockBaker.Balance -= transaction.BakerFee;
            blockBaker.StakingBalance -= transaction.BakerFee;

            sender.TransactionsCount--;
            if (target != null && target != sender) target.TransactionsCount--;

            sender.Counter = transaction.Counter - 1;
            (sender as User).Revealed = true;
            #endregion

            Db.TransactionOps.Remove(transaction);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        public virtual async Task RevertInternal(Block block, TransactionOperation transaction)
        {
            #region init
            transaction.Block ??= block;
            transaction.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            transaction.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            transaction.Sender = await Cache.Accounts.GetAsync(transaction.SenderId);
            transaction.Sender.Delegate ??= Cache.Accounts.GetDelegate(transaction.Sender.DelegateId);
            transaction.Target = await Cache.Accounts.GetAsync(transaction.TargetId);

            if (transaction.Target != null)
                transaction.Target.Delegate ??= Cache.Accounts.GetDelegate(transaction.Target.DelegateId);

            transaction.Initiator = await Cache.Accounts.GetAsync(transaction.InitiatorId);
            transaction.Initiator.Delegate ??= Cache.Accounts.GetDelegate(transaction.Initiator.DelegateId);
            #endregion

            #region entities
            var parentSender = transaction.Initiator;
            var parentDelegate = parentSender.Delegate ?? parentSender as Data.Models.Delegate;
            var sender = transaction.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            var target = transaction.Target;
            var targetDelegate = target?.Delegate ?? target as Data.Models.Delegate;

            //Db.TryAttach(block);
            //Db.TryAttach(parentTx);
            Db.TryAttach(parentSender);
            Db.TryAttach(parentDelegate);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(target);
            Db.TryAttach(targetDelegate);
            #endregion

            #region revert result
            if (transaction.Status == OperationStatus.Applied)
            {
                target.Balance -= transaction.Amount;

                if (targetDelegate != null)
                {
                    targetDelegate.StakingBalance -= transaction.Amount;
                    if (targetDelegate.Id != target.Id)
                        targetDelegate.DelegatedBalance -= transaction.Amount;
                }

                if (target is Data.Models.Delegate delegat)
                {
                    if (transaction.ResetDeactivation != null)
                    {
                        if (transaction.ResetDeactivation <= transaction.Level)
                            await UpdateDelegate(delegat, false);

                        delegat.DeactivationLevel = (int)transaction.ResetDeactivation;
                    }
                }

                sender.Balance += transaction.Amount;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += transaction.Amount;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance += transaction.Amount;
                }

                var spent = (transaction.StorageFee ?? 0) + (transaction.AllocationFee ?? 0);
                
                parentSender.Balance += spent;
                if (parentDelegate != null)
                {
                    parentDelegate.StakingBalance += spent;
                    if (parentDelegate.Id != parentSender.Id)
                        parentDelegate.DelegatedBalance += spent;
                }

                if (transaction.StorageId != null)
                    await RevertStorage(transaction);
            }
            #endregion

            #region revert operation
            sender.TransactionsCount--;
            if (target != null && target != sender) target.TransactionsCount--;
            if (parentSender != sender && parentSender != target) parentSender.TransactionsCount--;
            #endregion

            Db.TransactionOps.Remove(transaction);
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual bool HasAllocated(JsonElement result) => false;

        protected virtual async Task ResetGracePeriod(TransactionOperation transaction)
        {
            if (transaction.Target is Data.Models.Delegate delegat)
            {
                var newDeactivationLevel = delegat.Staked ? GracePeriod.Reset(transaction.Block) : GracePeriod.Init(transaction.Block);
                if (delegat.DeactivationLevel < newDeactivationLevel)
                {
                    if (delegat.DeactivationLevel <= transaction.Level)
                        await UpdateDelegate(delegat, true);

                    transaction.ResetDeactivation = delegat.DeactivationLevel;
                    delegat.DeactivationLevel = newDeactivationLevel;
                }
            }
        }

        protected virtual BlockEvents GetBlockEvents(Account target)
        {
            return target is Contract c && c.Kind == ContractKind.SmartContract
                ? BlockEvents.SmartContracts
                : BlockEvents.None;
        }

        protected virtual async Task ProcessParameters(TransactionOperation transaction, JsonElement parameters)
        {
            var (rawEp, rawParam) = ("default", Micheline.FromJson(parameters));

            if (transaction.Target is Contract contract)
            {
                if (contract.Kind == ContractKind.DelegatorContract)
                {
                    if (rawParam is MichelinePrim p && p.Prim == PrimType.Unit)
                        return;

                    transaction.Entrypoint = rawEp;
                    transaction.RawParameters = rawParam.ToBytes();
                }
                else
                {
                    try
                    {
                        var schema = await Cache.Schemas.GetAsync(contract);
                        var (normEp, normParam) = schema.NormalizeParameter(rawEp, rawParam);

                        transaction.Entrypoint = normEp;
                        transaction.RawParameters = schema.OptimizeParameter(normEp, normParam).ToBytes();
                        transaction.JsonParameters = schema.HumanizeParameter(normEp, normParam);
                    }
                    catch (Exception ex)
                    {
                        transaction.Entrypoint ??= rawEp;
                        transaction.RawParameters ??= rawParam.ToBytes();

                        if (transaction.Status == OperationStatus.Applied)
                            Logger.LogError($"Failed to humanize tx {transaction.OpHash} parameters: {ex.Message}");
                    }
                }
            }
            else
            {
                transaction.Entrypoint = rawEp;
                transaction.RawParameters = rawParam.ToBytes();
            }
        }

        protected virtual async Task ProcessStorage(TransactionOperation transaction, JsonElement storage)
        {
            if (transaction.Target is not Contract contract || contract.Kind == ContractKind.DelegatorContract)
                return;

            var schema = await Cache.Schemas.GetAsync(contract);
            var currentStorage = await Cache.Storages.GetAsync(contract);

            var newStorageMicheline = schema.OptimizeStorage(Micheline.FromJson(storage), false);
            newStorageMicheline = NormalizeStorage(transaction, newStorageMicheline, schema);
            var newStorageBytes = newStorageMicheline.ToBytes();

            if (newStorageBytes.IsEqual(currentStorage.RawValue))
            {
                Db.TryAttach(currentStorage);
                transaction.Storage = currentStorage;
                return;
            }

            var newStorage = new Storage
            {
                Id = Cache.AppState.NextStorageId(),
                Level = transaction.Level,
                ContractId = contract.Id,
                TransactionId = transaction.Id,
                RawValue = newStorageBytes,
                JsonValue = schema.HumanizeStorage(newStorageMicheline),
                Current = true,
            };

            Db.TryAttach(currentStorage);
            currentStorage.Current = false;

            Db.Storages.Add(newStorage);
            Cache.Storages.Add(contract, newStorage);

            transaction.Storage = newStorage;
        }

        public async Task RevertStorage(TransactionOperation transaction)
        {
            var contract = transaction.Target as Contract;
            var storage = await Cache.Storages.GetAsync(contract);

            if (storage.TransactionId == transaction.Id)
            {
                var prevStorage = await Db.Storages
                    .Where(x => x.ContractId == contract.Id && x.Id < storage.Id)
                    .OrderByDescending(x => x.Id)
                    .FirstAsync();

                prevStorage.Current = true;
                Cache.Storages.Add(contract, prevStorage);

                Db.Storages.Remove(storage);
                Cache.AppState.ReleaseStorageId();
            }
        }

        protected virtual IMicheline NormalizeStorage(TransactionOperation transaction, IMicheline storage, ContractScript schema)
        {
            var view = schema.Storage.Schema.ToTreeView(storage);
            var bigmap = view.Nodes().FirstOrDefault(x => x.Schema.Prim == PrimType.big_map);
            if (bigmap != null)
                storage = storage.Replace(bigmap.Value, new MichelineInt(transaction.Target.Id));
            return storage;
        }

        protected virtual IEnumerable<BigMapDiff> ParseBigMapDiffs(TransactionOperation transaction, JsonElement result)
        {
            if (transaction.Level != 5993)
                return null;
            // It seems there were no big_map diffs at all in proto 1
            // thus there was no an adequate way to track big_map updates,
            // so the only way to handle this single big_map update is hardcoding
            return new List<BigMapDiff>
            {
                new UpdateDiff
                {
                    Ptr = transaction.Target.Id,
                    KeyHash = "exprteAx9hWkXvYSQ4nN9SqjJGVR1sTneHQS1QEcSdzckYdXZVvsqY",
                    Key = new MichelineString("KT1R3uoZ6W1ZxEwzqtv75Ro7DhVY6UAcxuK2"),
                    Value = new MichelinePrim
                    {
                        Prim = PrimType.Pair,
                        Args = new List<IMicheline>
                        {
                            new MichelineString("Aliases Contract"),
                            new MichelinePrim
                            {
                                Prim = PrimType.Pair,
                                Args = new List<IMicheline>
                                {
                                    new MichelinePrim { Prim = PrimType.None },
                                    new MichelinePrim
                                    {
                                        Prim = PrimType.Pair,
                                        Args = new List<IMicheline>
                                        {
                                            new MichelineInt(0),
                                            new MichelinePrim
                                            {
                                                Prim = PrimType.Pair,
                                                Args = new List<IMicheline>
                                                {
                                                    new MichelinePrim
                                                    {
                                                        Prim = PrimType.Left,
                                                        Args = new List<IMicheline>
                                                        {
                                                            new MichelinePrim { Prim = PrimType.Unit }
                                                        }
                                                    },
                                                    new MichelineInt(1530741267)
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                }
            };
        }

        protected virtual int GetConsumedGas(JsonElement result)
        {
            return result.OptionalInt32("consumed_gas") ?? 0;
        }
    }
}
