using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class DoubleEndorsingCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
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

            var doubleEndorsing = new DoubleEndorsingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                SlashedLevel = block.Level,
                AccusedLevel = content.Required("op1").Required("operations").RequiredInt32("level") + 1,
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
            accuser.Balance += doubleEndorsing.Reward;
            accuser.StakingBalance += doubleEndorsing.Reward;

            offender.Balance -= doubleEndorsing.LostStaked;
            offender.StakingBalance -= doubleEndorsing.LostStaked;

            accuser.DoubleEndorsingCount++;
            if (offender != accuser) offender.DoubleEndorsingCount++;

            block.Operations |= Operations.DoubleEndorsings;

            Cache.AppState.Get().DoubleEndorsingOpsCount++;
            Cache.Statistics.Current.TotalBurned += doubleEndorsing.LostStaked - doubleEndorsing.Reward;
            Cache.Statistics.Current.TotalFrozen -= doubleEndorsing.LostStaked;
            #endregion

            Db.DoubleEndorsingOps.Add(doubleEndorsing);
            Context.DoubleEndorsingOps.Add(doubleEndorsing);
        }

        public virtual void Revert(Block block, DoubleEndorsingOperation doubleEndorsing)
        {
            #region entities
            var accuser = Cache.Accounts.GetDelegate(doubleEndorsing.AccuserId);
            var offender = Cache.Accounts.GetDelegate(doubleEndorsing.OffenderId);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance -= doubleEndorsing.Reward;
            accuser.StakingBalance -= doubleEndorsing.Reward;

            offender.Balance += doubleEndorsing.LostStaked;
            offender.StakingBalance += doubleEndorsing.LostStaked;

            accuser.DoubleEndorsingCount--;
            if (offender != accuser) offender.DoubleEndorsingCount--;

            Cache.AppState.Get().DoubleEndorsingOpsCount--;
            #endregion

            Db.DoubleEndorsingOps.Remove(doubleEndorsing);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
