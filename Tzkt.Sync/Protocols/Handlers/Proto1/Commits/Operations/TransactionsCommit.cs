using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;
using Netezos.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto1
{
    class TransactionsCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public TransactionOperation Transaction { get; private set; } = null!;
        public IEnumerable<BigMapDiff>? BigMapDiffs { get; private set; }
        public IEnumerable<TicketUpdates>? TicketUpdates { get; private set; }
        public Account? Target { get; private set; }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));
            var target = await Cache.Accounts.GetAsync(content.OptionalString("destination"));

            var result = content.Required("metadata").Required("operation_result");

            var transaction = new TransactionOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                Amount = content.RequiredInt64("amount"),
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                SenderId = sender.Id,
                TargetId = target?.Id,
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
                    ? result.OptionalInt32("paid_storage_size_diff") * Context.Protocol.ByteCost
                    : null,
                AllocationFee = HasAllocated(result)
                    ? (long?)Context.Protocol.OriginationSize * Context.Protocol.ByteCost
                    : null
            };


            if (target is not User && content.TryGetProperty("parameters", out var parameters))
                await ProcessParameters(transaction, target, parameters);
            #endregion

            #region entities
            var blockBaker = Context.Proposer;

            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;

            //var target = transaction.Target;
            var targetDelegate = target != null
                ? (Cache.Accounts.GetDelegate(target.DelegateId) ?? target as Data.Models.Delegate)
                : null;

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

            block.Operations |= Operations.Transactions;
            block.Fees += transaction.BakerFee;

            sender.Counter = transaction.Counter;

            Cache.AppState.Get().TransactionOpsCount++;
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

                target!.Balance += transaction.Amount;
                if (targetDelegate != null)
                {
                    targetDelegate.StakingBalance += transaction.Amount;
                    if (targetDelegate.Id != target.Id)
                        targetDelegate.DelegatedBalance += transaction.Amount;
                }

                await ResetGracePeriod(transaction, target);

                if (result.TryGetProperty("storage", out var storage))
                {
                    BigMapDiffs = ParseBigMapDiffs(transaction, result);
                    await ProcessStorage(transaction, target, storage);
                }

                TicketUpdates = ParseTicketUpdates("ticket_updates", result);
                
                if (target is SmartRollup)
                    Proto.Inbox.Push(transaction.Id);

                Cache.Statistics.Current.TotalBurned += burned;
                if (target.Id == NullAddress.Id)
                    Cache.Statistics.Current.TotalBanished += transaction.Amount;
            }
            #endregion

            Proto.Manager.Set(sender);
            //Db.TransactionOps.Add(transaction);
            Context.TransactionOps.Add(transaction);
            Transaction = transaction;
            Target = target;
        }

        public virtual async Task ApplyInternal(Block block, ManagerOperation parent, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));
            var target = await Cache.Accounts.GetAsync(content.OptionalString("destination"));

            var result = content.Required("result");

            var transaction = new TransactionOperation
            {
                Id = Cache.AppState.NextOperationId(),
                InitiatorId = parent.SenderId,
                Level = parent.Level,
                Timestamp = parent.Timestamp,
                OpHash = parent.OpHash,
                Counter = parent.Counter,
                Amount = content.RequiredInt64("amount"),
                Nonce = content.RequiredInt32("nonce"),
                SenderId = sender.Id,
                SenderCodeHash = (sender as Contract)?.CodeHash,
                TargetId = target?.Id,
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
                    ? result.OptionalInt32("paid_storage_size_diff") * Context.Protocol.ByteCost
                    : null,
                AllocationFee = HasAllocated(result)
                    ? (long?)Context.Protocol.OriginationSize * Context.Protocol.ByteCost
                    : null
            };

            if (target is not User && content.TryGetProperty("parameters", out var parameters))
                await ProcessParameters(transaction, target, parameters);
            #endregion

            #region entities
            var parentSender = await Cache.Accounts.GetAsync(parent.SenderId);
            var parentDelegate = Cache.Accounts.GetDelegate(parentSender.DelegateId) ?? parentSender as Data.Models.Delegate;
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;
            //var target = transaction.Target;
            var targetDelegate = target != null
                ? (Cache.Accounts.GetDelegate(target.DelegateId) ?? target as Data.Models.Delegate)
                : null;

            //Db.TryAttach(parentTx);
            //Db.TryAttach(parentSender);
            Db.TryAttach(parentDelegate);
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

            block.Operations |= Operations.Transactions;

            Cache.AppState.Get().TransactionOpsCount++;
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

                target!.Balance += transaction.Amount;
                if (target.Id == parentSender.Id)
                    Proto.Manager.Credit(transaction.Amount);

                if (targetDelegate != null)
                {
                    targetDelegate.StakingBalance += transaction.Amount;
                    if (targetDelegate.Id != target.Id)
                        targetDelegate.DelegatedBalance += transaction.Amount;
                }

                await ResetGracePeriod(transaction, target);

                if (result.TryGetProperty("storage", out var storage))
                {
                    BigMapDiffs = ParseBigMapDiffs(transaction, result);
                    await ProcessStorage(transaction, target, storage);
                }
                
                TicketUpdates = ParseTicketUpdates("ticket_receipt", result);

                if (target is SmartRollup)
                    Proto.Inbox.Push(transaction.Id);

                Cache.Statistics.Current.TotalBurned += burned;
                if (target.Id == NullAddress.Id)
                    Cache.Statistics.Current.TotalBanished += transaction.Amount;
            }
            #endregion

            //Db.TransactionOps.Add(transaction);
            Context.TransactionOps.Add(transaction);
            Transaction = transaction;
            Target = target;
        }

        public virtual async Task Revert(Block block, TransactionOperation transaction)
        {
            #region entities
            var blockBaker = Context.Proposer;
            var sender = await Cache.Accounts.GetAsync(transaction.SenderId);
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;
            var target = await Cache.Accounts.GetAsync(transaction.TargetId);
            var targetDelegate = target != null
                ? (Cache.Accounts.GetDelegate(target.DelegateId) ?? target as Data.Models.Delegate)
                : null;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(target);
            Db.TryAttach(targetDelegate);
            #endregion

            #region revert result
            if (transaction.Status == OperationStatus.Applied)
            {
                target!.Balance -= transaction.Amount;

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
                    await RevertStorage(transaction, (target as Contract)!);
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
            if (sender is User user) user.Revealed = true;

            Cache.AppState.Get().TransactionOpsCount--;
            #endregion

            //Db.TransactionOps.Remove(transaction);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        public virtual async Task RevertInternal(Block block, TransactionOperation transaction)
        {
            #region entities
            var parentSender = await Cache.Accounts.GetAsync(transaction.InitiatorId!.Value);
            var parentDelegate = Cache.Accounts.GetDelegate(parentSender.DelegateId) ?? parentSender as Data.Models.Delegate;
            var sender = await Cache.Accounts.GetAsync(transaction.SenderId);
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;
            var target = await Cache.Accounts.GetAsync(transaction.TargetId);
            var targetDelegate = target != null
                ? (Cache.Accounts.GetDelegate(target.DelegateId) ?? target as Data.Models.Delegate)
                : null;

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
                target!.Balance -= transaction.Amount;

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
                    await RevertStorage(transaction, (target as Contract)!);
            }
            #endregion

            #region revert operation
            sender.TransactionsCount--;
            if (target != null && target != sender) target.TransactionsCount--;
            if (parentSender != sender && parentSender != target) parentSender.TransactionsCount--;

            Cache.AppState.Get().TransactionOpsCount--;
            #endregion

            //Db.TransactionOps.Remove(transaction);
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual bool HasAllocated(JsonElement result) => false;

        protected virtual async Task ResetGracePeriod(TransactionOperation transaction, Account target)
        {
            if (target is Data.Models.Delegate delegat)
            {
                var newDeactivationLevel = delegat.Staked ? GracePeriod.Reset(transaction.Level, Context.Protocol) : GracePeriod.Init(transaction.Level, Context.Protocol);
                if (delegat.DeactivationLevel < newDeactivationLevel)
                {
                    if (delegat.DeactivationLevel <= transaction.Level)
                        await UpdateDelegate(delegat, true);

                    transaction.ResetDeactivation = delegat.DeactivationLevel;
                    delegat.DeactivationLevel = newDeactivationLevel;
                }
            }
        }

        protected virtual async Task ProcessParameters(TransactionOperation transaction, Account? target, JsonElement parameters)
        {
            var (rawEp, rawParam) = ("default", Micheline.FromJson(parameters)!);

            if (target is Contract contract)
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
                            Logger.LogError(ex, "Failed to humanize tx {hash} parameters", transaction.OpHash);
                    }
                }
            }
            else
            {
                transaction.Entrypoint = rawEp;
                transaction.RawParameters = rawParam.ToBytes();
            }
        }

        protected virtual async Task ProcessStorage(TransactionOperation transaction, Account target, JsonElement storage)
        {
            if (target is not Contract contract || contract.Kind == ContractKind.DelegatorContract)
                return;

            var schema = await Cache.Schemas.GetAsync(contract);
            var currentStorage = await Cache.Storages.GetAsync(contract);

            var newStorageMicheline = schema.OptimizeStorage(Micheline.FromJson(storage)!, false);
            newStorageMicheline = NormalizeStorage(transaction, newStorageMicheline, schema);
            var newStorageBytes = newStorageMicheline.ToBytes();

            if (newStorageBytes.IsEqual(currentStorage.RawValue))
            {
                transaction.StorageId = currentStorage.Id;
                return;
            }

            Db.TryAttach(currentStorage);
            currentStorage.Current = false;

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

            Db.Storages.Add(newStorage);
            Cache.Storages.Add(contract, newStorage);

            transaction.StorageId = newStorage.Id;
        }

        public async Task RevertStorage(TransactionOperation transaction, Contract contract)
        {
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
                storage = storage.Replace(bigmap.Value, new MichelineInt(transaction.TargetId!.Value));
            return storage;
        }

        protected virtual IEnumerable<BigMapDiff>? ParseBigMapDiffs(TransactionOperation transaction, JsonElement result)
        {
            if (transaction.Level != 5993)
                return null;
            // It seems there were no big_map diffs at all in proto 1
            // thus there was no an adequate way to track big_map updates,
            // so the only way to handle this single big_map update is hardcoding
            return
            [
                new UpdateDiff
                {
                    Ptr = transaction.TargetId!.Value,
                    KeyHash = "exprteAx9hWkXvYSQ4nN9SqjJGVR1sTneHQS1QEcSdzckYdXZVvsqY",
                    Key = new MichelineString("KT1R3uoZ6W1ZxEwzqtv75Ro7DhVY6UAcxuK2"),
                    Value = new MichelinePrim
                    {
                        Prim = PrimType.Pair,
                        Args =
                        [
                            new MichelineString("Aliases Contract"),
                            new MichelinePrim
                            {
                                Prim = PrimType.Pair,
                                Args =
                                [
                                    new MichelinePrim { Prim = PrimType.None },
                                    new MichelinePrim
                                    {
                                        Prim = PrimType.Pair,
                                        Args =
                                        [
                                            new MichelineInt(0),
                                            new MichelinePrim
                                            {
                                                Prim = PrimType.Pair,
                                                Args =
                                                [
                                                    new MichelinePrim
                                                    {
                                                        Prim = PrimType.Left,
                                                        Args =
                                                        [
                                                            new MichelinePrim { Prim = PrimType.Unit }
                                                        ]
                                                    },
                                                    new MichelineInt(1530741267)
                                                ]
                                            }
                                        ]
                                    }
                                ]
                            }
                        ]
                    },
                }
            ];
        }

        protected virtual int GetConsumedGas(JsonElement result)
        {
            return result.OptionalInt32("consumed_gas") ?? 0;
        }

        protected virtual IEnumerable<TicketUpdates>? ParseTicketUpdates(string property, JsonElement result)
        {
            if (!result.TryGetProperty(property, out var ticketUpdates))
                return null;

            var res = new List<TicketUpdates>();
            foreach (var updates in ticketUpdates.RequiredArray().EnumerateArray())
            {
                var list = new List<TicketUpdate>();
                foreach (var update in updates.RequiredArray("updates").EnumerateArray())
                {
                    var amount = update.RequiredBigInteger("amount");
                    if (amount != BigInteger.Zero)
                    {
                        list.Add(new TicketUpdate
                        {
                            Account = update.RequiredString("account"),
                            Amount = amount
                        });
                    }
                }

                if (list.Count > 0)
                {
                    var ticketToken = updates.Required("ticket_token");
                    var type = Micheline.FromJson(ticketToken.Required("content_type"))!;
                    var value = Micheline.FromJson(ticketToken.Required("content"))!;
                    var rawType = type.ToBytes();

                    byte[] rawContent;
                    string? jsonContent;

                    try
                    {
                        var schema = Schema.Create((type as MichelinePrim)!);
                        rawContent = schema.Optimize(value).ToBytes();
                        jsonContent = schema.Humanize(value);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to parse ticket content");
                        rawContent = value.ToBytes();
                        jsonContent = null;
                    }

                    res.Add(new TicketUpdates
                    {
                        Ticket = new TicketIdentity
                        {
                            Ticketer = ticketToken.RequiredString("ticketer"),
                            RawType = rawType,
                            RawContent = rawContent,
                            JsonContent = jsonContent,
                            TypeHash = Script.GetHash(rawType),
                            ContentHash = Script.GetHash(rawContent)
                        },
                        Updates = list
                    });
                }
            }

            return res.Count > 0 ? res : null;
        }
    }
}
