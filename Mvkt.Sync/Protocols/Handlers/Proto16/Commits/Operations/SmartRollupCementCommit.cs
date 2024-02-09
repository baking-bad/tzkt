﻿using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mvkt.Data.Models;
using Mvkt.Data.Models.Base;

namespace Mvkt.Sync.Protocols.Proto16
{
    class SmartRollupCementCommit : ProtocolCommit
    {
        public SmartRollupCementCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);
            var rollup = await Cache.Accounts.GetSmartRollupOrDefaultAsync(content.RequiredString("rollup"));
            var commitment = await Cache.SmartRollupCommitments.GetOrDefaultAsync(GetCommitment(content), rollup?.Id);

            var result = content.Required("metadata").Required("operation_result");

            var operation = new SmartRollupCementOperation
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
                SmartRollupId = rollup?.Id,
                CommitmentId = commitment?.Id,
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
            Db.TryAttach(rollup);
            Db.TryAttach(commitment);
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

            sender.SmartRollupCementCount++;
            if (rollup != null) rollup.SmartRollupCementCount++;

            block.Operations |= Operations.SmartRollupCement;
            block.Fees += operation.BakerFee;

            sender.Counter = operation.Counter;

            if (commitment != null)
                commitment.LastLevel = operation.Level;

            Cache.AppState.Get().SmartRollupCementOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                rollup.InboxLevel = commitment.InboxLevel;
                rollup.LastCommitment = commitment.Hash;
                rollup.CementedCommitments++;
                rollup.PendingCommitments--;
                
                commitment.Status = SmartRollupCommitmentStatus.Cemented;
            }
            #endregion

            Proto.Manager.Set(operation.Sender);
            Db.SmartRollupCementOps.Add(operation);
        }

        public virtual async Task Revert(Block block, SmartRollupCementOperation operation)
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
            var rollup = await Cache.Accounts.GetAsync(operation.SmartRollupId) as SmartRollup;
            var commitment = await Cache.SmartRollupCommitments.GetOrDefaultAsync(operation.CommitmentId);

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(rollup);
            Db.TryAttach(commitment);
            #endregion

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                var prevCement = await Db.SmartRollupCementOps.AsNoTracking()
                    .Where(x => x.Id < operation.Id && x.SmartRollupId == operation.SmartRollupId && x.Status == OperationStatus.Applied)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();
                var prevCementedCommitment = await Cache.SmartRollupCommitments.GetOrDefaultAsync(prevCement?.CommitmentId);

                rollup.InboxLevel = prevCementedCommitment?.InboxLevel ?? 0;
                rollup.LastCommitment = prevCementedCommitment?.Hash ?? rollup.GenesisCommitment;
                rollup.CementedCommitments--;
                rollup.PendingCommitments++;

                commitment.Status = SmartRollupCommitmentStatus.Pending;
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

            sender.SmartRollupCementCount--;
            if (rollup != null) rollup.SmartRollupCementCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User).Revealed = true;

            // commitment.LastLevel is not reverted

            Cache.AppState.Get().SmartRollupCementOpsCount--;
            #endregion

            Db.SmartRollupCementOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual string GetCommitment(JsonElement content) => content.RequiredString("commitment");
    }
}
