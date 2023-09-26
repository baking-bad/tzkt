using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class DoubleEndorsingCommit : ProtocolCommit
    {
        public DoubleEndorsingCommit(ProtocolHandler protocol) : base(protocol) { }

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

            var doubleEndorsing = new DoubleEndorsingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                AccusedLevel = content.Required("op1").Required("operations").RequiredInt32("level") + 1,
                Accuser = block.Proposer,
                Offender = Cache.Accounts.GetDelegate(offenderAddr),

                AccuserReward = accuserReward,
                OffenderLossOwn = offenderLoss
            };
            #endregion

            #region entities
            var accuser = doubleEndorsing.Accuser;
            var offender = doubleEndorsing.Offender;
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance += doubleEndorsing.AccuserReward;
            accuser.StakingBalance += doubleEndorsing.AccuserReward;

            offender.Balance -= doubleEndorsing.OffenderLossOwn;
            offender.StakingBalance -= doubleEndorsing.OffenderLossOwn;

            accuser.DoubleEndorsingCount++;
            if (offender != accuser) offender.DoubleEndorsingCount++;

            block.Operations |= Operations.DoubleEndorsings;

            Cache.Statistics.Current.TotalBurned += doubleEndorsing.OffenderLossOwn - doubleEndorsing.AccuserReward;
            Cache.Statistics.Current.TotalFrozen -= doubleEndorsing.OffenderLossOwn;
            #endregion

            Db.DoubleEndorsingOps.Add(doubleEndorsing);
        }

        public virtual void Revert(Block block, DoubleEndorsingOperation doubleEndorsing)
        {
            #region init
            doubleEndorsing.Block ??= block;
            doubleEndorsing.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            doubleEndorsing.Accuser ??= Cache.Accounts.GetDelegate(doubleEndorsing.AccuserId);
            doubleEndorsing.Offender ??= Cache.Accounts.GetDelegate(doubleEndorsing.OffenderId);
            #endregion

            #region entities
            var accuser = doubleEndorsing.Accuser;
            var offender = doubleEndorsing.Offender;
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance -= doubleEndorsing.AccuserReward;
            accuser.StakingBalance -= doubleEndorsing.AccuserReward;

            offender.Balance += doubleEndorsing.OffenderLossOwn;
            offender.StakingBalance += doubleEndorsing.OffenderLossOwn;

            accuser.DoubleEndorsingCount--;
            if (offender != accuser) offender.DoubleEndorsingCount--;
            #endregion

            Db.DoubleEndorsingOps.Remove(doubleEndorsing);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
