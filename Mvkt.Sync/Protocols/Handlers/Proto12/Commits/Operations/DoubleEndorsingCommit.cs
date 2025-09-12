using System.Text.Json;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto12
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

                SlashedLevel = block.Level,
                AccusedLevel = content.Required("op1").Required("operations").RequiredInt32("level") + 1,
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
            var accuser = doubleEndorsing.Accuser;
            var offender = doubleEndorsing.Offender;
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

            Cache.Statistics.Current.TotalBurned += doubleEndorsing.LostStaked - doubleEndorsing.Reward;
            Cache.Statistics.Current.TotalFrozen -= doubleEndorsing.LostStaked;
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
            accuser.Balance -= doubleEndorsing.Reward;
            accuser.StakingBalance -= doubleEndorsing.Reward;

            offender.Balance += doubleEndorsing.LostStaked;
            offender.StakingBalance += doubleEndorsing.LostStaked;

            accuser.DoubleEndorsingCount--;
            if (offender != accuser) offender.DoubleEndorsingCount--;
            #endregion

            Db.DoubleEndorsingOps.Remove(doubleEndorsing);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
