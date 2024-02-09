using System.Text.Json;
using Netmavryk.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto16
{
    class SmartRollupAddMessagesCommit : ProtocolCommit
    {
        public SmartRollupAddMessagesCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);

            var result = content.Required("metadata").Required("operation_result");

            var operation = new SmartRollupAddMessagesOperation
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
                MessagesCount = 0,
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

            sender.SmartRollupAddMessagesCount++;

            block.Operations |= Operations.SmartRollupAddMessages;
            block.Fees += operation.BakerFee;

            sender.Counter = operation.Counter;

            Cache.AppState.Get().SmartRollupAddMessagesOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                foreach (var message in content.RequiredArray("message").EnumerateArray())
                {
                    Proto.Inbox.Push(operation.Id, Hex.Parse(message.RequiredString()));
                    operation.MessagesCount++;
                }
            }
            #endregion

            Proto.Manager.Set(operation.Sender);
            Db.SmartRollupAddMessagesOps.Add(operation);
        }

        public virtual async Task Revert(Block block, SmartRollupAddMessagesOperation operation)
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

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            #endregion

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
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

            sender.SmartRollupAddMessagesCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User).Revealed = true;

            Cache.AppState.Get().SmartRollupAddMessagesOpsCount--;
            #endregion

            Db.SmartRollupAddMessagesOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
