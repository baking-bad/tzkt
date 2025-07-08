using System.Text.Json;
using Netezos.Keys;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto15
{
    class UpdateSecondaryKeyCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));

            var pubKey = content.RequiredString("pk");
            var pubKeyHash = PubKey.FromBase58(pubKey).Address;
            var result = content.Required("metadata").Required("operation_result");
            var operation = new UpdateSecondaryKeyOperation
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
                KeyType = content.RequiredString("kind") switch
                {
                    "update_consensus_key" => SecondaryKeyType.Consensus,
                    "update_companion_key" => SecondaryKeyType.Companion,
                    _ => throw new NotImplementedException()
                },
                ActivationCycle = block.Cycle + Context.Protocol.ConsensusRightsDelay + 1,
                PublicKey = pubKey,
                PublicKeyHash = pubKeyHash,
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
            var blockBaker = Context.Proposer;
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
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

            sender.UpdateSecondaryKeyCount++;

            block.Operations |= Operations.UpdateSecondaryKey;
            block.Fees += operation.BakerFee;

            sender.Counter = operation.Counter;

            Cache.AppState.Get().UpdateSecondaryKeyOpsCount++;
            #endregion

            #region apply result
            #endregion

            Proto.Manager.Set(sender);
            Db.UpdateSecondaryKeyOps.Add(operation);
            Context.UpdateSecondaryKeyOps.Add(operation);
        }

        public virtual async Task Revert(Block block, UpdateSecondaryKeyOperation operation)
        {
            #region entities
            var blockBaker = Context.Proposer;
            var sender = await Cache.Accounts.GetAsync(operation.SenderId);
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            #endregion

            #region revert result
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

            sender.UpdateSecondaryKeyCount--;

            sender.Counter = operation.Counter - 1;

            Cache.AppState.Get().UpdateSecondaryKeyOpsCount--;
            #endregion

            Db.UpdateSecondaryKeyOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
