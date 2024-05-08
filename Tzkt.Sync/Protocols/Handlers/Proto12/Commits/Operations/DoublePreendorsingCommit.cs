using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class DoublePreendorsingCommit : ProtocolCommit
    {
        public DoublePreendorsingCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual void Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var balanceUpdates = content.Required("metadata").RequiredArray("balance_updates").EnumerateArray();
            var freezerUpdates = balanceUpdates.Where(x => x.RequiredString("kind") == "freezer" && x.RequiredString("category") == "deposits");
            var contractUpdates = balanceUpdates.Where(x => x.RequiredString("kind") == "contract");

            var offenderAddr = freezerUpdates.Any()
                ? freezerUpdates.First().RequiredString("delegate")
                : block.Proposer.Address; // this is wrong, but no big deal

            var offenderLoss = freezerUpdates.Any()
                ? -freezerUpdates.Sum(x => x.RequiredInt64("change"))
                : 0;

            var accuserReward = contractUpdates.Any()
                ? contractUpdates.Sum(x => x.RequiredInt64("change"))
                : 0;

            var doublePreendorsing = new DoublePreendorsingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                SlashedLevel = block.Level,
                AccusedLevel = content.Required("op1").Required("operations").RequiredInt32("level"),
                Accuser = block.Proposer,
                Offender = Cache.Accounts.GetDelegate(offenderAddr),

                Reward = accuserReward,
                LostStaked = offenderLoss,
                LostUnstaked = 0,
                LostExternalStaked = 0,
                LostExternalUnstaked = 0
            };
            #endregion

            #region entities
            var accuser = doublePreendorsing.Accuser;
            var offender = doublePreendorsing.Offender;
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance += doublePreendorsing.Reward;
            accuser.StakingBalance += doublePreendorsing.Reward;

            offender.Balance -= doublePreendorsing.LostStaked;
            offender.StakingBalance -= doublePreendorsing.LostStaked;

            accuser.DoublePreendorsingCount++;
            if (offender != accuser) offender.DoublePreendorsingCount++;

            block.Operations |= Operations.DoublePreendorsings;

            Cache.Statistics.Current.TotalBurned += doublePreendorsing.LostStaked - doublePreendorsing.Reward;
            Cache.Statistics.Current.TotalFrozen -= doublePreendorsing.LostStaked;
            #endregion

            Db.DoublePreendorsingOps.Add(doublePreendorsing);
        }

        public virtual void Revert(Block block, DoublePreendorsingOperation doublePreendorsing)
        {
            #region init
            doublePreendorsing.Block ??= block;
            doublePreendorsing.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            doublePreendorsing.Accuser ??= Cache.Accounts.GetDelegate(doublePreendorsing.AccuserId);
            doublePreendorsing.Offender ??= Cache.Accounts.GetDelegate(doublePreendorsing.OffenderId);
            #endregion

            #region entities
            var accuser = doublePreendorsing.Accuser;
            var offender = doublePreendorsing.Offender;
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance -= doublePreendorsing.Reward;
            accuser.StakingBalance -= doublePreendorsing.Reward;

            offender.Balance += doublePreendorsing.LostStaked;
            offender.StakingBalance += doublePreendorsing.LostStaked;

            accuser.DoublePreendorsingCount--;
            if (offender != accuser) offender.DoublePreendorsingCount--;
            #endregion

            Db.DoublePreendorsingOps.Remove(doublePreendorsing);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
