using System.Text.Json;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto19
{
    class DalPublishCommitmentCommit : ProtocolCommit
    {
        public DalPublishCommitmentCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source")) as User;
            var senderDelegate = sender as Data.Models.Delegate ?? Cache.Accounts.GetDelegate(sender.DelegateId);

            var result = content.Required("metadata").Required("operation_result");
            var operation = new DalPublishCommitmentOperation
            {
                Id = Cache.AppState.NextOperationId(),
                OpHash = op.RequiredString("hash"),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                Sender = sender,
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
            sender.Balance -= operation.BakerFee;
            sender.Counter = operation.Counter;
            sender.DalPublishCommitmentOpsCount++;

            if (senderDelegate != null)
            {
                Db.TryAttach(senderDelegate);
                senderDelegate.StakingBalance -= operation.BakerFee;
                if (senderDelegate != sender)
                    senderDelegate.DelegatedBalance -= operation.BakerFee;
            }

            block.Proposer.Balance += operation.BakerFee;
            block.Proposer.StakingBalance += operation.BakerFee;

            block.Operations |= Operations.DalPublishCommitment;
            block.Fees += operation.BakerFee;

            Cache.AppState.Get().DalPublishCommitmentOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                var commitmentStatus = new DalCommitmentStatus
                {
                    PublishmentId = operation.Id,
                };
                Db.DalCommitmentStatus.Add(commitmentStatus);
            }
            #endregion

            Proto.Manager.Set(operation.Sender);
            Db.DalPublishCommitmentOps.Add(operation);
        }

        public async Task Revert(Block block, DalPublishCommitmentOperation operation)
        {
            var sender = await Cache.Accounts.GetAsync(operation.SenderId) as User;
            var senderDelegate = sender as Data.Models.Delegate ?? Cache.Accounts.GetDelegate(sender.DelegateId);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                Db.DalCommitmentStatus.Remove(operation.DalCommitmentStatus);
            }
            #endregion

            #region revert operation
            sender.Balance += operation.BakerFee;
            sender.Counter = operation.Counter - 1;
            sender.DalPublishCommitmentOpsCount--;

            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance += operation.BakerFee;
                if (senderDelegate != sender)
                    senderDelegate.DelegatedBalance += operation.BakerFee;
            }

            block.Proposer.Balance -= operation.BakerFee;
            block.Proposer.StakingBalance -= operation.BakerFee;

            Cache.AppState.Get().DalPublishCommitmentOpsCount--;
            #endregion

            Db.DalPublishCommitmentOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
