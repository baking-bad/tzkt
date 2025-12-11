using System.Text.Json;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto19
{
    class DalPublishCommitmentCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = (await Cache.Accounts.GetExistingAsync(content.RequiredString("source")) as User)!;

            var result = content.Required("metadata").Required("operation_result");
            var operation = new DalPublishCommitmentOperation
            {
                Id = Cache.AppState.NextOperationId(),
                OpHash = op.RequiredString("hash"),
                Level = block.Level,
                Timestamp = block.Timestamp,
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                SenderId = sender.Id,
                Slot = content.Required("slot_header").RequiredInt32("slot_index"),
                Commitment = content.Required("slot_header").RequiredString("commitment"),
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
                AllocationFee = null,
                StorageFee = null,
                StorageUsed = 0
            };
            #endregion

            #region apply operation
            Db.TryAttach(sender);
            PayFee(sender, operation.BakerFee);
            sender.Counter = operation.Counter;
            sender.DalPublishCommitmentOpsCount++;

            block.Operations |= Operations.DalPublishCommitment;

            Cache.AppState.Get().DalPublishCommitmentOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                // nothing to do
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.DalPublishCommitmentOps.Add(operation);
            Context.DalPublishCommitmentOps.Add(operation);
        }

        public async Task Revert(Block block, DalPublishCommitmentOperation operation)
        {
            var sender = (await Cache.Accounts.GetAsync(operation.SenderId) as User)!;
            Db.TryAttach(sender);

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                // nothing to do
            }
            #endregion

            #region revert operation
            RevertPayFee(sender, operation.BakerFee);
            sender.Counter = operation.Counter - 1;
            sender.DalPublishCommitmentOpsCount--;

            Cache.AppState.Get().DalPublishCommitmentOpsCount--;
            #endregion

            Db.DalPublishCommitmentOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
