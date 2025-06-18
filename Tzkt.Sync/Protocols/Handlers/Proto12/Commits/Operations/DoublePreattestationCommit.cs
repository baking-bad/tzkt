using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class DoublePreattestationCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual void Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var balanceUpdates = content.Required("metadata").RequiredArray("balance_updates").EnumerateArray();
            var freezerUpdates = balanceUpdates.Where(x => x.RequiredString("kind") == "freezer" && x.RequiredString("category") == "deposits");
            var contractUpdates = balanceUpdates.Where(x => x.RequiredString("kind") == "contract");

            var offenderAddr = freezerUpdates.Any()
                ? freezerUpdates.First().RequiredString("delegate")
                : Context.Proposer.Address; // this is wrong, but no big deal

            var offenderLoss = freezerUpdates.Any()
                ? -freezerUpdates.Sum(x => x.RequiredInt64("change"))
                : 0;

            var accuserReward = contractUpdates.Any()
                ? contractUpdates.Sum(x => x.RequiredInt64("change"))
                : 0;

            var accuser = Context.Proposer;
            var offender = Cache.Accounts.GetExistingDelegate(offenderAddr);

            var doublePreattestation = new DoublePreattestationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                SlashedLevel = block.Level,
                AccusedLevel = content.Required("op1").Required("operations").RequiredInt32("level"),
                AccuserId = accuser.Id,
                OffenderId = offender.Id,

                Reward = accuserReward,
                LostStaked = offenderLoss,
                LostUnstaked = 0,
                LostExternalStaked = 0,
                LostExternalUnstaked = 0
            };
            #endregion

            #region entities
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance += doublePreattestation.Reward;
            accuser.StakingBalance += doublePreattestation.Reward;

            offender.Balance -= doublePreattestation.LostStaked;
            offender.StakingBalance -= doublePreattestation.LostStaked;

            accuser.DoublePreattestationCount++;
            if (offender != accuser) offender.DoublePreattestationCount++;

            block.Operations |= Operations.DoublePreattestations;

            Cache.AppState.Get().DoublePreattestationOpsCount++;
            Cache.Statistics.Current.TotalBurned += doublePreattestation.LostStaked - doublePreattestation.Reward;
            Cache.Statistics.Current.TotalFrozen -= doublePreattestation.LostStaked;
            #endregion

            Db.DoublePreattestationOps.Add(doublePreattestation);
            Context.DoublePreattestationOps.Add(doublePreattestation);
        }

        public virtual void Revert(Block block, DoublePreattestationOperation doublePreattestation)
        {
            #region entities
            var accuser = Cache.Accounts.GetDelegate(doublePreattestation.AccuserId);
            var offender = Cache.Accounts.GetDelegate(doublePreattestation.OffenderId);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance -= doublePreattestation.Reward;
            accuser.StakingBalance -= doublePreattestation.Reward;

            offender.Balance += doublePreattestation.LostStaked;
            offender.StakingBalance += doublePreattestation.LostStaked;

            accuser.DoublePreattestationCount--;
            if (offender != accuser) offender.DoublePreattestationCount--;

            Cache.AppState.Get().DoublePreattestationOpsCount--;
            #endregion

            Db.DoublePreattestationOps.Remove(doublePreattestation);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
