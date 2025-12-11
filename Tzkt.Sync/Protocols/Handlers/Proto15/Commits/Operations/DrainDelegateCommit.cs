using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto15
{
    class DrainDelegateCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var delegat = Cache.Accounts.GetExistingDelegate(content.RequiredString("delegate"));
            var target = (await Cache.Accounts.GetAsync(content.RequiredString("destination")))!;

            var balanceUpdates = content.Required("metadata").RequiredArray("balance_updates").EnumerateArray();

            var allocationFeeUpdate = balanceUpdates.SingleOrDefault(x => x.RequiredString("kind") == "burned");
            var allocationFee = allocationFeeUpdate.ValueKind != JsonValueKind.Undefined
                ? allocationFeeUpdate.RequiredInt64("change")
                : 0;

            var deposits = balanceUpdates
                .Where(x => x.RequiredString("kind") == "contract" && x.RequiredInt64("change") > 0)
                .OrderByDescending(x => x.RequiredInt64("change"))
                .ToList();

            var amount = 0L;
            var fee = 0L;

            if (deposits.Count == 2)
            {
                amount = deposits.First(x => x.RequiredString("contract") == target.Address).RequiredInt64("change");
                fee = deposits.Last(x => x.RequiredString("contract") == Context.Proposer.Address).RequiredInt64("change");
            }
            else if (deposits.Count == 1)
            {
                if (deposits[0].RequiredString("contract") == target.Address)
                    amount = deposits[0].RequiredInt64("change");
                else if (deposits[0].RequiredString("contract") == Context.Proposer.Address)
                    fee = deposits[0].RequiredInt64("change");
                else
                    throw new Exception("Unexpected balance updates behavior");
            }
            else if (deposits.Count != 0)
            {
                throw new Exception("Unexpected balance updates behavior");
            }

            var operation = new DrainDelegateOperation
            {
                Id = Cache.AppState.NextOperationId(),
                OpHash = op.RequiredString("hash"),
                Level = block.Level,
                Timestamp = block.Timestamp,
                DelegateId = delegat.Id,
                TargetId = target.Id,
                Amount = amount,
                Fee = fee,
                AllocationFee = allocationFee
            };
            #endregion

            #region entities
            Db.TryAttach(delegat);
            Db.TryAttach(target);
            #endregion

            #region apply operation
            PayFee(delegat, operation.Fee);

            Spend(delegat, delegat, operation.Amount + operation.AllocationFee);

            Receive(target, operation.Amount);

            delegat.DrainDelegateCount++;
            if (target != delegat) target.DrainDelegateCount++;

            block.Operations |= Operations.DrainDelegate;

            Cache.AppState.Get().DrainDelegateOpsCount++;

            Cache.Statistics.Current.TotalBurned += operation.AllocationFee;
            #endregion

            Db.DrainDelegateOps.Add(operation);
            Context.DrainDelegateOps.Add(operation);
        }

        public virtual async Task Revert(Block block, DrainDelegateOperation operation)
        {
            #region entities
            var delegat = Cache.Accounts.GetDelegate(operation.DelegateId);
            Db.TryAttach(delegat);
            
            var target = await Cache.Accounts.GetAsync(operation.TargetId);
            Db.TryAttach(target);
            #endregion

            #region apply operation
            RevertPayFee(delegat, operation.Fee);

            RevertSpend(delegat, delegat, operation.Amount + operation.AllocationFee);

            RevertReceive(target, operation.Amount);

            delegat.DrainDelegateCount--;
            if (target != delegat) target.DrainDelegateCount--;

            Cache.AppState.Get().DrainDelegateOpsCount--;
            #endregion

            Db.DrainDelegateOps.Remove(operation);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
