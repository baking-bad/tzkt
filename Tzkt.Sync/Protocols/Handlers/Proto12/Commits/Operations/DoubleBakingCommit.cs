using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class DoubleBakingCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
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

            var doubleBaking = new DoubleBakingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                SlashedLevel = block.Level,
                AccusedLevel = content.Required("bh1").RequiredInt32("level"),
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
            Receive(accuser, accuser, doubleBaking.Reward);
            
            Spend(offender, offender, doubleBaking.LostStaked);

            accuser.DoubleBakingCount++;
            if (offender != accuser) offender.DoubleBakingCount++;

            block.Operations |= Operations.DoubleBakings;

            Cache.AppState.Get().DoubleBakingOpsCount++;
            Cache.Statistics.Current.TotalBurned += doubleBaking.LostStaked - doubleBaking.Reward;
            Cache.Statistics.Current.TotalFrozen -= doubleBaking.LostStaked;
            #endregion

            Db.DoubleBakingOps.Add(doubleBaking);
            Context.DoubleBakingOps.Add(doubleBaking);
        }

        public virtual void Revert(Block block, DoubleBakingOperation doubleBaking)
        {
            #region entities
            var accuser = Cache.Accounts.GetDelegate(doubleBaking.AccuserId);
            var offender = Cache.Accounts.GetDelegate(doubleBaking.OffenderId);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            RevertReceive(accuser, accuser, doubleBaking.Reward);

            RevertSpend(offender, offender, doubleBaking.LostStaked);

            accuser.DoubleBakingCount--;
            if (offender != accuser) offender.DoubleBakingCount--;

            Cache.AppState.Get().DoubleBakingOpsCount--;
            #endregion

            Db.DoubleBakingOps.Remove(doubleBaking);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
