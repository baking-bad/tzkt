using System.Text.Json;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto13
{
    class TxRollupCommitCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));
            var rollup = await Cache.Accounts.GetAsync(content.RequiredString("rollup"));

            var result = content.Required("metadata").Required("operation_result");
            var bond = result.RequiredArray("balance_updates").EnumerateArray()
                .FirstOrDefault(x => x.RequiredString("kind") == "contract");

            var operation = new TxRollupCommitOperation
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
                RollupId = rollup?.Id,
                Bond = bond.ValueKind == JsonValueKind.Undefined ? 0 : -bond.RequiredInt64("change"),
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
                GasUsed = (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000)
            };
            #endregion

            #region entities
            Db.TryAttach(sender);
            Db.TryAttach(rollup);
            #endregion

            #region apply operation
            PayFee(sender, operation.BakerFee);

            sender.TxRollupCommitCount++;
            if (rollup != null) rollup.TxRollupCommitCount++;

            block.Operations |= Operations.TxRollupCommit;

            sender.Counter = operation.Counter;

            Cache.AppState.Get().TxRollupCommitOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                sender.RollupBonds += operation.Bond;
                rollup!.RollupBonds += operation.Bond;

                Cache.Statistics.Current.TotalRollupBonds += operation.Bond;
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.TxRollupCommitOps.Add(operation);
            Context.TxRollupCommitOps.Add(operation);
        }

        public virtual async Task Revert(Block block, TxRollupCommitOperation operation)
        {
            #region entities
            var sender = await Cache.Accounts.GetAsync(operation.SenderId);
            var rollup = await Cache.Accounts.GetAsync(operation.RollupId);

            Db.TryAttach(sender);
            Db.TryAttach(rollup);
            #endregion

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                sender.RollupBonds -= operation.Bond;
                rollup!.RollupBonds -= operation.Bond;
            }
            #endregion

            #region revert operation
            RevertPayFee(sender, operation.BakerFee);

            sender.TxRollupCommitCount--;
            if (rollup != null) rollup.TxRollupCommitCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User)!.Revealed = true;

            Cache.AppState.Get().TxRollupCommitOpsCount--;
            #endregion

            Db.TxRollupCommitOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
