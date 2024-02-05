using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto16
{
    class SmartRollupRecoverBondCommit : ProtocolCommit
    {
        public SmartRollupRecoverBondCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);
            var rollup = await Cache.Accounts.GetSmartRollupOrDefaultAsync(content.RequiredString("rollup"));
            var staker = await Cache.Accounts.GetAsync(content.RequiredString("staker"));

            var result = content.Required("metadata").Required("operation_result");
            var bond = result.OptionalArray("balance_updates")?.EnumerateArray()
                .FirstOrDefault(x => x.RequiredString("kind") == "contract") ?? default;

            var operation = new SmartRollupRecoverBondOperation
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
                StakerId = staker?.Id,
                Bond = bond.ValueKind == JsonValueKind.Undefined ? 0 : bond.RequiredInt64("change"),
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
            Db.TryAttach(staker);
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

            sender.SmartRollupRecoverBondCount++;
            if (rollup != null) rollup.SmartRollupRecoverBondCount++;
            if (staker != null && staker.Id != sender.Id) staker.SmartRollupRecoverBondCount++;

            block.Operations |= Operations.SmartRollupRecoverBond;
            block.Fees += operation.BakerFee;

            sender.Counter = operation.Counter;

            Cache.AppState.Get().SmartRollupRecoverBondOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                staker.SmartRollupBonds -= operation.Bond;
                rollup.SmartRollupBonds -= operation.Bond;
                rollup.ActiveStakers--;

                var bondOp = block.SmartRollupPublishOps?
                    .FirstOrDefault(x => x.SmartRollupId == operation.SmartRollupId && x.BondStatus == SmartRollupBondStatus.Active && x.SenderId == operation.StakerId)
                    ?? await Db.SmartRollupPublishOps.FirstAsync(x => x.SmartRollupId == operation.SmartRollupId && x.BondStatus == SmartRollupBondStatus.Active && x.SenderId == operation.StakerId);
                bondOp.BondStatus = SmartRollupBondStatus.Returned;

                Cache.Statistics.Current.TotalSmartRollupBonds -= operation.Bond;
            }
            #endregion

            Proto.Manager.Set(operation.Sender);
            Db.SmartRollupRecoverBondOps.Add(operation);
        }

        public virtual async Task Revert(Block block, SmartRollupRecoverBondOperation operation)
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
            var staker = await Cache.Accounts.GetAsync(operation.StakerId);

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(rollup);
            Db.TryAttach(staker);
            #endregion

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                staker.SmartRollupBonds += operation.Bond;
                rollup.SmartRollupBonds += operation.Bond;
                rollup.ActiveStakers++;

                var bondOp = await Db.SmartRollupPublishOps
                    .OrderByDescending(x => x.Id)
                    .FirstAsync(x => x.SmartRollupId == operation.SmartRollupId && x.BondStatus == SmartRollupBondStatus.Returned && x.SenderId == operation.StakerId);
                bondOp.BondStatus = SmartRollupBondStatus.Active;
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

            sender.SmartRollupRecoverBondCount--;
            if (rollup != null) rollup.SmartRollupRecoverBondCount--;
            if (staker != null && staker.Id != sender.Id) staker.SmartRollupRecoverBondCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User).Revealed = true;

            Cache.AppState.Get().SmartRollupRecoverBondOpsCount--;
            #endregion

            Db.SmartRollupRecoverBondOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
