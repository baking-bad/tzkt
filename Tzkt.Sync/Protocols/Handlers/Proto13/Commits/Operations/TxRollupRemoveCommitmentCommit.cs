using System.Text.Json;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto13
{
    class TxRollupRemoveCommitmentCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));
            var rollup = await Cache.Accounts.GetAsync(content.RequiredString("rollup"));

            var result = content.Required("metadata").Required("operation_result");

            var operation = new TxRollupRemoveCommitmentOperation
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

            sender.TxRollupRemoveCommitmentCount++;
            if (rollup != null) rollup.TxRollupRemoveCommitmentCount++;

            block.Operations |= Operations.TxRollupRemoveCommitment;

            sender.Counter = operation.Counter;

            Cache.AppState.Get().TxRollupRemoveCommitmentOpsCount++;
            #endregion

            #region apply result
            //if (operation.Status == OperationStatus.Applied)
            //{
            //}
            #endregion

            Proto.Manager.Set(sender);
            Db.TxRollupRemoveCommitmentOps.Add(operation);
            Context.TxRollupRemoveCommitmentOps.Add(operation);
        }

        public virtual async Task Revert(Block block, TxRollupRemoveCommitmentOperation operation)
        {
            #region entities
            var sender = await Cache.Accounts.GetAsync(operation.SenderId);
            var rollup = await Cache.Accounts.GetAsync(operation.RollupId);

            Db.TryAttach(sender);
            Db.TryAttach(rollup);
            #endregion

            #region revert result
            //if (operation.Status == OperationStatus.Applied)
            //{
            //}
            #endregion

            #region revert operation
            RevertPayFee(sender, operation.BakerFee);

            sender.TxRollupRemoveCommitmentCount--;
            if (rollup != null) rollup.TxRollupRemoveCommitmentCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User)!.Revealed = true;

            Cache.AppState.Get().TxRollupRemoveCommitmentOpsCount--;
            #endregion

            Db.TxRollupRemoveCommitmentOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
