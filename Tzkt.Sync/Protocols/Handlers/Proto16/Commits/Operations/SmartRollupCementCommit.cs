using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto16
{
    class SmartRollupCementCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));
            var rollup = await Cache.Accounts.GetSmartRollupOrDefaultAsync(content.RequiredString("rollup"));
            var commitment = await Cache.SmartRollupCommitments.GetOrDefaultAsync(GetCommitment(content), rollup?.Id);

            var result = content.Required("metadata").Required("operation_result");

            var operation = new SmartRollupCementOperation
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
            Db.TryAttach(sender);
            Db.TryAttach(rollup);
            Db.TryAttach(commitment);
            #endregion

            #region apply operation
            PayFee(sender, operation.BakerFee);

            sender.SmartRollupCementCount++;
            if (rollup != null) rollup.SmartRollupCementCount++;

            block.Operations |= Operations.SmartRollupCement;

            sender.Counter = operation.Counter;

            if (commitment != null)
                commitment.LastLevel = operation.Level;

            Cache.AppState.Get().SmartRollupCementOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                rollup!.InboxLevel = commitment!.InboxLevel;
                rollup.LastCommitment = commitment.Hash;
                rollup.CementedCommitments++;
                rollup.PendingCommitments--;
                
                commitment.Status = SmartRollupCommitmentStatus.Cemented;
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.SmartRollupCementOps.Add(operation);
            Context.SmartRollupCementOps.Add(operation);
        }

        public virtual async Task Revert(Block block, SmartRollupCementOperation operation)
        {
            #region entities
            var sender = await Cache.Accounts.GetAsync(operation.SenderId);
            var rollup = await Cache.Accounts.GetAsync(operation.SmartRollupId) as SmartRollup;
            var commitment = await Cache.SmartRollupCommitments.GetOrDefaultAsync(operation.CommitmentId);

            Db.TryAttach(sender);
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

                rollup!.InboxLevel = prevCementedCommitment?.InboxLevel ?? 0;
                rollup.LastCommitment = prevCementedCommitment?.Hash ?? rollup.GenesisCommitment;
                rollup.CementedCommitments--;
                rollup.PendingCommitments++;

                commitment!.Status = SmartRollupCommitmentStatus.Pending;
            }
            #endregion

            #region revert operation
            RevertPayFee(sender, operation.BakerFee);

            sender.SmartRollupCementCount--;
            if (rollup != null) rollup.SmartRollupCementCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User)!.Revealed = true;

            // commitment.LastLevel is not reverted

            Cache.AppState.Get().SmartRollupCementOpsCount--;
            #endregion

            Db.SmartRollupCementOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual string? GetCommitment(JsonElement content) => content.RequiredString("commitment");
    }
}
