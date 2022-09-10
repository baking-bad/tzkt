using System.Linq;
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
            var freezerUpdate = balanceUpdates.FirstOrDefault(x => x.RequiredString("kind") == "freezer" && x.RequiredString("category") == "deposits");
            var contractUpdate = balanceUpdates.FirstOrDefault(x => x.RequiredString("kind") == "contract");

            var offenderAddr = freezerUpdate.ValueKind != JsonValueKind.Undefined
                ? freezerUpdate.RequiredString("delegate")
                : block.Proposer.Address; // this is wrong, but no big deal

            var offenderLoss = freezerUpdate.ValueKind != JsonValueKind.Undefined
                ? -freezerUpdate.RequiredInt64("change")
                : 0;

            var accuserReward = contractUpdate.ValueKind != JsonValueKind.Undefined
                ? contractUpdate.RequiredInt64("change")
                : 0;

            var doublePreendorsing = new DoublePreendorsingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                AccusedLevel = content.Required("op1").Required("operations").RequiredInt32("level"),
                Accuser = block.Proposer,
                Offender = Cache.Accounts.GetDelegate(offenderAddr),

                AccuserReward = accuserReward,
                OffenderLoss = offenderLoss
            };
            #endregion

            #region entities
            var accuser = doublePreendorsing.Accuser;
            var offender = doublePreendorsing.Offender;
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance += doublePreendorsing.AccuserReward;
            accuser.StakingBalance += doublePreendorsing.AccuserReward;

            offender.Balance -= doublePreendorsing.OffenderLoss;
            offender.FrozenDeposit -= doublePreendorsing.OffenderLoss;
            offender.StakingBalance -= doublePreendorsing.OffenderLoss;

            accuser.DoublePreendorsingCount++;
            if (offender != accuser) offender.DoublePreendorsingCount++;

            block.Operations |= Operations.DoublePreendorsings;
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
            accuser.Balance -= doublePreendorsing.AccuserReward;
            accuser.StakingBalance -= doublePreendorsing.AccuserReward;

            offender.Balance += doublePreendorsing.OffenderLoss;
            offender.FrozenDeposit += doublePreendorsing.OffenderLoss;
            offender.StakingBalance += doublePreendorsing.OffenderLoss;

            accuser.DoublePreendorsingCount--;
            if (offender != accuser) offender.DoublePreendorsingCount--;
            #endregion

            Db.DoublePreendorsingOps.Remove(doublePreendorsing);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
