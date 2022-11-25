using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto15
{
    class DrainDelegateCommit : ProtocolCommit
    {
        public DrainDelegateCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var delegat = Cache.Accounts.GetDelegate(content.RequiredString("delegate"));
            var target = await Cache.Accounts.GetAsync(content.RequiredString("destination"));

            var balanceUpdates = content.Required("metadata").RequiredArray("balance_updates").EnumerateArray();

            var amountUpdate = balanceUpdates.FirstOrDefault(x => x.RequiredString("contract") == target.Address);
            var amount = amountUpdate.ValueKind != JsonValueKind.Undefined
                ? amountUpdate.RequiredInt64("change")
                : 0;

            var feeUpdate = balanceUpdates.FirstOrDefault(x => x.RequiredString("contract") == block.Proposer.Address);
            var fee = feeUpdate.ValueKind != JsonValueKind.Undefined
                ? feeUpdate.RequiredInt64("change")
                : 0;

            var operation = new DrainDelegateOperation
            {
                Id = Cache.AppState.NextOperationId(),
                OpHash = op.RequiredString("hash"),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                DelegateId = delegat.Id,
                TargetId = target.Id,
                Amount = amount,
                Fee = fee
            };
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            var targetDelegate = Cache.Accounts.GetDelegate(target.DelegateId) ?? target as Data.Models.Delegate;

            Db.TryAttach(blockBaker);
            Db.TryAttach(delegat);
            Db.TryAttach(target);
            Db.TryAttach(targetDelegate);
            #endregion

            #region apply operation
            delegat.Balance -= operation.Amount;
            delegat.StakingBalance -= operation.Amount;

            delegat.Balance -= operation.Fee;
            delegat.StakingBalance -= operation.Fee;

            target.Balance += operation.Amount;
            if (targetDelegate != null)
            {
                targetDelegate.StakingBalance += operation.Amount;
                if (targetDelegate.Id != target.Id)
                    targetDelegate.DelegatedBalance += operation.Amount;
            }

            blockBaker.Balance += operation.Fee;
            blockBaker.StakingBalance += operation.Fee;

            delegat.DrainDelegateCount++;
            if (target != delegat) target.DrainDelegateCount++;

            block.Operations |= Operations.DrainDelegate;
            block.Fees += operation.Fee;

            Cache.AppState.Get().DrainDelegateOpsCount++;
            #endregion

            Db.DrainDelegateOps.Add(operation);
        }

        public virtual async Task Revert(Block block, DrainDelegateOperation operation)
        {
            #region entities
            var blockBaker = Cache.Accounts.GetDelegate(block.ProposerId);
            Db.TryAttach(blockBaker);
            
            var delegat = Cache.Accounts.GetDelegate(operation.DelegateId);
            Db.TryAttach(delegat);
            
            var target = await Cache.Accounts.GetAsync(operation.TargetId);
            Db.TryAttach(target);
            
            var targetDelegate = Cache.Accounts.GetDelegate(target.DelegateId) ?? target as Data.Models.Delegate;
            Db.TryAttach(targetDelegate);
            #endregion

            #region apply operation
            delegat.Balance += operation.Amount;
            delegat.StakingBalance += operation.Amount;

            delegat.Balance += operation.Fee;
            delegat.StakingBalance += operation.Fee;

            target.Balance -= operation.Amount;
            if (targetDelegate != null)
            {
                targetDelegate.StakingBalance -= operation.Amount;
                if (targetDelegate.Id != target.Id)
                    targetDelegate.DelegatedBalance -= operation.Amount;
            }

            blockBaker.Balance -= operation.Fee;
            blockBaker.StakingBalance -= operation.Fee;

            delegat.DrainDelegateCount--;
            if (target != delegat) target.DrainDelegateCount--;

            Cache.AppState.Get().DrainDelegateOpsCount--;
            #endregion

            Db.DrainDelegateOps.Remove(operation);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
