using System.Text.Json;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto16
{
    class SmartRollupPublishCommit : ProtocolCommit
    {
        public SmartRollupPublishCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);
            var smartRollup = await Cache.Accounts.GetAsync(content.RequiredString("rollup"));

            var commitment = content.Required("commitment");
            var result = content.Required("metadata").Required("operation_result");
            var bond = result.RequiredArray("balance_updates").EnumerateArray()
                .FirstOrDefault(x => x.RequiredString("kind") == "contract");

            var operation = new SmartRollupPublishOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                SenderId = sender.Id,
                Sender = sender,
                SmartRollupId = smartRollup?.Id,
                Bond = bond.ValueKind == JsonValueKind.Undefined ? 0 : -bond.RequiredInt64("change"),
                Commitment = result.RequiredString("staked_hash"),
                Predecessor = commitment.RequiredString("predecessor"),
                State = commitment.RequiredString("compressed_state"),
                InboxLevel = commitment.RequiredInt32("inbox_level"),
                Ticks = commitment.RequiredInt64("number_of_ticks"),
                CommitmentStatus = SmartRollupCommitmentStatus.Pending,
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
                StorageUsed = 0,
                StorageFee = null,
                AllocationFee = null
            };
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            Db.TryAttach(block.Proposer);
            Db.TryAttach(sender);
            Db.TryAttach(sender.Delegate);
            Db.TryAttach(smartRollup);
            #endregion

            #region apply operation
            sender.Balance -= operation.BakerFee;
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance -= operation.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance -= operation.BakerFee;
            }
            blockBaker.Balance += operation.BakerFee;
            blockBaker.StakingBalance += operation.BakerFee;

            sender.SmartRollupPublishCount++;
            if (smartRollup != null) smartRollup.SmartRollupPublishCount++;

            block.Operations |= Operations.SmartRollupPublish;
            block.Fees += operation.BakerFee;

            sender.Counter = operation.Counter;

            Cache.AppState.Get().SmartRollupPublishOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                sender.SmartRollupBonds += operation.Bond;
                smartRollup.SmartRollupBonds += operation.Bond;
                (smartRollup as SmartRollup).PendingCommitments++;
            }
            #endregion

            Proto.Manager.Set(operation.Sender);
            Db.SmartRollupPublishOps.Add(operation);
        }

        public virtual async Task Revert(Block block, SmartRollupPublishOperation operation)
        {
            #region init
            operation.Block ??= block;
            operation.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            operation.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            operation.Sender ??= await Cache.Accounts.GetAsync(operation.SenderId);
            operation.Sender.Delegate ??= Cache.Accounts.GetDelegate(operation.Sender.DelegateId);
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            var sender = operation.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            var smartRollup = await Cache.Accounts.GetAsync(operation.SmartRollupId) as SmartRollup;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(smartRollup);
            #endregion

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                sender.RollupBonds -= operation.Bond;
                smartRollup.RollupBonds -= operation.Bond;
                smartRollup.PendingCommitments--;
            }
            #endregion

            #region revert operation
            sender.Balance += operation.BakerFee;
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance += operation.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance += operation.BakerFee;
            }
            blockBaker.Balance -= operation.BakerFee;
            blockBaker.StakingBalance -= operation.BakerFee;

            sender.SmartRollupPublishCount--;
            if (smartRollup != null) smartRollup.SmartRollupPublishCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User).Revealed = true;

            Cache.AppState.Get().SmartRollupPublishOpsCount--;
            #endregion

            Db.SmartRollupPublishOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
