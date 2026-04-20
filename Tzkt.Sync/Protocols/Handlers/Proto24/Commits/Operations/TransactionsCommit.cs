using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;
using Netezos.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto24
{
    class TransactionsCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public TransactionOperation Transaction { get; private set; } = null!;
        public IEnumerable<BigMapDiff>? BigMapDiffs { get; private set; }
        public IEnumerable<TicketUpdates>? TicketUpdates { get; private set; }
        public Account? Target { get; private set; }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            if (Cache.AppState.Get().BlocksCount <= 3)
            {
                if (content.RequiredString("source") == NullAddress.Address)
                {
                    if (await Cache.Accounts.ExistsAsync(content.RequiredString("destination"), AccountType.User))
                    {
                        var to = await Cache.Accounts.GetAsync(content.RequiredString("destination"));
                        if (to!.Balance == content.RequiredInt64("amount"))
                            return;
                    }
                }
            }

            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));
            var target = await Cache.Accounts.GetOrCreateAsync(content.RequiredString("destination"));

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
                TargetId = target.Id,
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
                GasUsed = (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000),
                StorageUsed = result.OptionalInt32("paid_storage_size_diff") ?? 0,
                StorageFee = result.OptionalInt32("paid_storage_size_diff") > 0
                    ? result.OptionalInt32("paid_storage_size_diff") * Context.Protocol.ByteCost
                    : null,
                AllocationFee = result.OptionalBool("allocated_destination_contract") == true
                    ? (long?)Context.Protocol.OriginationSize * Context.Protocol.ByteCost
                    : null
            };

            if (target is not User && content.TryGetProperty("parameters", out var parameters))
                await ProcessParameters(transaction, target, parameters);
            #endregion

            #region apply operation
            Db.TryAttach(sender);
            PayFee(sender, transaction.BakerFee);
            sender.Counter = transaction.Counter;
            sender.TransactionsCount++;

            if (target != sender)
            {
                Db.TryAttach(target);
                target.TransactionsCount++;
            }

            block.Operations |= Operations.Transactions;

            Cache.AppState.Get().TransactionOpsCount++;
            #endregion

            #region apply result
            if (transaction.Status == OperationStatus.Applied)
            {
                var burned = (transaction.StorageFee ?? 0) + (transaction.AllocationFee ?? 0);
                Proto.Manager.Burn(burned);

                Spend(sender, transaction.Amount + burned);

                Receive(target, transaction.Amount);

                if (result.TryGetProperty("storage", out var storage))
                {
                    BigMapDiffs = ParseBigMapDiffs(transaction, result);
                    await ProcessStorage(transaction, target, storage);
                }

                await ApplyAddressRegistryDiffs(transaction, result);

                TicketUpdates = ParseTicketUpdates("ticket_updates", result);

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
            var parentSender = await Cache.Accounts.GetAsync(parent.SenderId);
            var sender = await Cache.Accounts.GetOrCreateAsync(content.RequiredString("source"));
            var target = await Cache.Accounts.GetOrCreateAsync(content.RequiredString("destination"));

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
                TargetId = target.Id,
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
                GasUsed = (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000),
                StorageUsed = result.OptionalInt32("paid_storage_size_diff") ?? 0,
                StorageFee = result.OptionalInt32("paid_storage_size_diff") > 0
                    ? result.OptionalInt32("paid_storage_size_diff") * Context.Protocol.ByteCost
                    : null,
                AllocationFee = result.OptionalBool("allocated_destination_contract") == true
                    ? (long?)Context.Protocol.OriginationSize * Context.Protocol.ByteCost
                    : null
            };

            if (target is not User && content.TryGetProperty("parameters", out var parameters))
                await ProcessParameters(transaction, target, parameters);
            #endregion

            #region apply operation
            if (parent is TransactionOperation parentTx)
            {
                parentTx.InternalOperations = (short?)((parentTx.InternalOperations ?? 0) + 1);
                parentTx.InternalTransactions = (short?)((parentTx.InternalTransactions ?? 0) + 1);
            }

            Db.TryAttach(sender);
            sender.TransactionsCount++;

            if (target != sender)
            {
                Db.TryAttach(target);
                target.TransactionsCount++;
            }

            if (parentSender != sender && parentSender != target)
                parentSender.TransactionsCount++;

            block.Operations |= Operations.Transactions;

            Cache.AppState.Get().TransactionOpsCount++;
            #endregion

            #region apply result
            if (transaction.Status == OperationStatus.Applied)
            {
                var burned = (transaction.StorageFee ?? 0) + (transaction.AllocationFee ?? 0);
                Proto.Manager.Burn(burned);

                Spend(parentSender, burned);

                Spend(sender, transaction.Amount);

                Receive(target, transaction.Amount);

                if (target == parentSender)
                    Proto.Manager.Credit(transaction.Amount);

                if (result.TryGetProperty("storage", out var storage))
                {
                    BigMapDiffs = ParseBigMapDiffs(transaction, result);
                    await ProcessStorage(transaction, target, storage);
                }

                await ApplyAddressRegistryDiffs(transaction, result);

                TicketUpdates = ParseTicketUpdates("ticket_receipt", result);

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
            var sender = await Cache.Accounts.GetAsync(transaction.SenderId);
            var target = await Cache.Accounts.GetAsync(transaction.TargetId);

            Db.TryAttach(sender);
            Db.TryAttach(target);
            #endregion

            #region revert result
            if (transaction.Status == OperationStatus.Applied)
            {
                RevertReceive(target, transaction.Amount);

                RevertSpend(sender, transaction.Amount + (transaction.StorageFee ?? 0) + (transaction.AllocationFee ?? 0));

                if (transaction.StorageId != null)
                    await RevertStorage(transaction, (target as Contract)!);

                await RevertAddressRegistryDiffs(transaction);
            }
            #endregion

            #region revert operation
            RevertPayFee(sender, transaction.BakerFee);

            sender.TransactionsCount--;
            if (target != sender) target.TransactionsCount--;

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
            var sender = await Cache.Accounts.GetAsync(transaction.SenderId);
            var target = await Cache.Accounts.GetAsync(transaction.TargetId);

            Db.TryAttach(parentSender);
            Db.TryAttach(sender);
            Db.TryAttach(target);
            #endregion

            #region revert result
            if (transaction.Status == OperationStatus.Applied)
            {
                RevertReceive(target, transaction.Amount);

                RevertSpend(sender, transaction.Amount);

                RevertSpend(parentSender, (transaction.StorageFee ?? 0) + (transaction.AllocationFee ?? 0));

                if (transaction.StorageId != null)
                    await RevertStorage(transaction, (target as Contract)!);

                await RevertAddressRegistryDiffs(transaction);
            }
            #endregion

            #region revert operation
            sender.TransactionsCount--;
            if (target != sender) target.TransactionsCount--;
            if (parentSender != sender && parentSender != target) parentSender.TransactionsCount--;

            Cache.AppState.Get().TransactionOpsCount--;
            #endregion

            //Db.TransactionOps.Remove(transaction);
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual async Task ProcessParameters(TransactionOperation transaction, Account target, JsonElement param)
        {
            string? rawEp = null;
            IMicheline? rawParam;
            try
            {
                rawEp = param.RequiredString("entrypoint");
                rawParam = Micheline.FromJson(param.Required("value"))!;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to parse tx parameters");
                transaction.Entrypoint = rawEp ?? string.Empty;
                transaction.RawParameters = new MichelineArray().ToBytes();
                return;
            }

            if (target is Contract contract)
            {
                var schema = contract.Kind > ContractKind.DelegatorContract
                    ? (await Cache.Schemas.GetAsync(contract))
                    : Script.ManagerTz;

                try
                {
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

        protected virtual IEnumerable<BigMapDiff>? ParseBigMapDiffs(TransactionOperation transaction, JsonElement result)
        {
            return result.TryGetProperty("lazy_storage_diff", out var diffs)
                ? BigMapDiff.ParseLazyStorage(diffs)
                : null;
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

        protected virtual async Task ApplyAddressRegistryDiffs(TransactionOperation transaction, JsonElement result)
        {
            if (result.TryGetProperty("address_registry_diff", out var diffs))
            {
                var minIndex = int.MaxValue;
                foreach (var diff in diffs.EnumerateArray())
                {
                    var address = diff.RequiredString("address");
                    var index = diff.RequiredInt32("index");

                    var account = await Cache.Accounts.GetOrCreateAsync(address);
                    if (account.Index != null)
                    {
                        if (account.Index != index)
                            throw new Exception("Address registry contains duplicates");

                        continue;
                    }

                    Db.TryAttach(account);
                    account.Index = index;

                    if (index < minIndex)
                        minIndex = index;
                }

                if (minIndex != int.MaxValue)
                    transaction.AddressRegistryIndex = minIndex;
            }
        }

        protected virtual async Task RevertAddressRegistryDiffs(TransactionOperation transaction)
        {
            if (transaction.AddressRegistryIndex is int minIndex)
            {
                var accounts = await Db.Accounts
                    .Where(x => x.Index != null && x.Index >= minIndex)
                    .ToListAsync();

                foreach (var account in accounts)
                {
                    Cache.Accounts.Add(account);
                    account.Index = null;
                }
            }
        }
    }
}
