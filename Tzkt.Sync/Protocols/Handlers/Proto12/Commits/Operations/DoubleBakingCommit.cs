using System.Linq;
using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class DoubleBakingCommit : ProtocolCommit
    {
        public DoubleBakingCommit(ProtocolHandler protocol) : base(protocol) { }

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

            var doubleBaking = new DoubleBakingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                AccusedLevel = content.Required("bh1").RequiredInt32("level"),
                Accuser = block.Proposer,
                Offender = Cache.Accounts.GetDelegate(offenderAddr),

                AccuserReward = accuserReward,
                OffenderLoss = offenderLoss
            };
            #endregion

            #region entities
            var accuser = doubleBaking.Accuser;
            var offender = doubleBaking.Offender;
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance += doubleBaking.AccuserReward;
            accuser.StakingBalance += doubleBaking.AccuserReward;

            offender.Balance -= doubleBaking.OffenderLoss;
            offender.FrozenDeposit -= doubleBaking.OffenderLoss;
            offender.StakingBalance -= doubleBaking.OffenderLoss;

            accuser.DoubleBakingCount++;
            if (offender != accuser) offender.DoubleBakingCount++;

            block.Operations |= Operations.DoubleBakings;
            #endregion

            Db.DoubleBakingOps.Add(doubleBaking);
        }

        public virtual void Revert(Block block, DoubleBakingOperation doubleBaking)
        {
            #region init
            doubleBaking.Block ??= block;
            doubleBaking.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            doubleBaking.Accuser ??= Cache.Accounts.GetDelegate(doubleBaking.AccuserId);
            doubleBaking.Offender ??= Cache.Accounts.GetDelegate(doubleBaking.OffenderId);
            #endregion

            #region entities
            var accuser = doubleBaking.Accuser;
            var offender = doubleBaking.Offender;
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance -= doubleBaking.AccuserReward;
            accuser.StakingBalance -= doubleBaking.AccuserReward;

            offender.Balance += doubleBaking.OffenderLoss;
            offender.FrozenDeposit += doubleBaking.OffenderLoss;
            offender.StakingBalance += doubleBaking.OffenderLoss;

            accuser.DoubleBakingCount--;
            if (offender != accuser) offender.DoubleBakingCount--;
            #endregion

            Db.DoubleBakingOps.Remove(doubleBaking);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
