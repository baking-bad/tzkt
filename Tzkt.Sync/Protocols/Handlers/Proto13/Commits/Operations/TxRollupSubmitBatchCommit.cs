﻿using System.Text.Json;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto13
{
    class TxRollupSubmitBatchCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));
            var rollup = await Cache.Accounts.GetAsync(content.RequiredString("rollup"));

            var result = content.Required("metadata").Required("operation_result");

            var operation = new TxRollupSubmitBatchOperation
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
                GasUsed = (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000),
                StorageUsed = result.OptionalInt32("paid_storage_size_diff") ?? 0,
                StorageFee = result.OptionalInt32("paid_storage_size_diff") > 0
                    ? result.OptionalInt32("paid_storage_size_diff") * Context.Protocol.ByteCost
                    : null
            };
            #endregion

            #region entities
            var blockBaker = Context.Proposer;
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(rollup);
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

            sender.TxRollupSubmitBatchCount++;
            if (rollup != null) rollup.TxRollupSubmitBatchCount++;

            block.Operations |= Operations.TxRollupSubmitBatch;
            block.Fees += operation.BakerFee;

            sender.Counter = operation.Counter;

            Cache.AppState.Get().TxRollupSubmitBatchOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                var burned = operation.StorageFee ?? 0;
                Proto.Manager.Burn(burned);

                sender.Balance -= burned;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= burned;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance -= burned;
                }

                Cache.Statistics.Current.TotalBurned += burned;
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.TxRollupSubmitBatchOps.Add(operation);
            Context.TxRollupSubmitBatchOps.Add(operation);
        }

        public virtual async Task Revert(Block block, TxRollupSubmitBatchOperation operation)
        {
            #region entities
            var blockBaker = Context.Proposer;
            var sender = await Cache.Accounts.GetAsync(operation.SenderId);
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;
            var rollup = await Cache.Accounts.GetAsync(operation.RollupId);

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(rollup);
            #endregion

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                var spent = operation.StorageFee ?? 0;

                sender.Balance += spent;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += spent;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance += spent;
                }
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

            sender.TxRollupSubmitBatchCount--;
            if (rollup != null) rollup.TxRollupSubmitBatchCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User)!.Revealed = true;

            Cache.AppState.Get().TxRollupSubmitBatchOpsCount--;
            #endregion

            Db.TxRollupSubmitBatchOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
