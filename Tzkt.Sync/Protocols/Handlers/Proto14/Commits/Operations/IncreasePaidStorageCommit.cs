using System.Numerics;
using System.Text.Json;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto14
{
    class IncreasePaidStorageCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));
            var contract = await Cache.Accounts.GetAsync(content.RequiredString("destination")) as Contract;

            var result = content.Required("metadata").Required("operation_result");
            var balanceUpdate = result.OptionalArray("balance_updates")?.EnumerateArray()
                .FirstOrDefault(x => x.RequiredString("kind") == "burned" && x.RequiredString("category") == "storage fees");
            var storageFee = balanceUpdate is JsonElement el && el.ValueKind != JsonValueKind.Undefined
                ? el.RequiredInt64("change")
                : 0;

            var operation = new IncreasePaidStorageOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                SenderId = sender.Id,
                ContractId = contract?.Id,
                Amount = BigInteger.Parse(content.RequiredString("amount")),
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
                StorageUsed = (int)(storageFee / Context.Protocol.ByteCost),
                StorageFee = storageFee
            };
            #endregion

            #region entities
            Db.TryAttach(sender);
            Db.TryAttach(contract);
            #endregion

            #region apply operation
            PayFee(sender, operation.BakerFee);

            sender.IncreasePaidStorageCount++;
            if (contract != null) contract.IncreasePaidStorageCount++;

            block.Operations |= Operations.IncreasePaidStorage;

            sender.Counter = operation.Counter;

            Cache.AppState.Get().IncreasePaidStorageOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                var burned = operation.StorageFee ?? 0;
                Proto.Manager.Burn(burned);

                Spend(sender, burned);

                Cache.Statistics.Current.TotalBurned += burned;
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.IncreasePaidStorageOps.Add(operation);
            Context.IncreasePaidStorageOps.Add(operation);
        }

        public virtual async Task Revert(Block block, IncreasePaidStorageOperation operation)
        {
            #region entities
            var sender = await Cache.Accounts.GetAsync(operation.SenderId);
            var contract = await Cache.Accounts.GetAsync(operation.ContractId);

            Db.TryAttach(sender);
            Db.TryAttach(contract);
            #endregion

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                RevertSpend(sender, operation.StorageFee ?? 0);
            }
            #endregion

            #region revert operation
            RevertPayFee(sender, operation.BakerFee);

            sender.IncreasePaidStorageCount--;
            if (contract != null) contract.IncreasePaidStorageCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User)!.Revealed = true;

            Cache.AppState.Get().IncreasePaidStorageOpsCount--;
            #endregion

            Db.IncreasePaidStorageOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
