using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class DoubleBakingCommit : ProtocolCommit
    {
        public DoubleBakingCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var balanceUpdates = content.Required("metadata").RequiredArray("balance_updates").EnumerateArray();

            var offenderAddr = balanceUpdates
                .First(x => x.RequiredInt64("change") < 0).RequiredString("delegate");

            var rewards = balanceUpdates
                .FirstOrDefault(x => x.RequiredString("category")[0] == 'r' && x.RequiredInt64("change") > 0);

            var lostDeposits = balanceUpdates
                .FirstOrDefault(x => x.RequiredString("category")[0] == 'd' && x.RequiredInt64("change") < 0);

            var lostRewards = balanceUpdates
                .FirstOrDefault(x => x.RequiredString("category")[0] == 'r' && x.RequiredInt64("change") < 0);

            var lostFees = balanceUpdates
                .FirstOrDefault(x => x.RequiredString("category")[0] == 'f' && x.RequiredInt64("change") < 0);

            var doubleBaking = new DoubleBakingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                AccusedLevel = content.Required("bh1").RequiredInt32("level"),
                Accuser = block.Baker,
                Offender = Cache.Accounts.GetDelegate(offenderAddr),

                AccuserReward = rewards.ValueKind != JsonValueKind.Undefined ? rewards.RequiredInt64("change") : 0,
                OffenderLostDeposit = lostDeposits.ValueKind != JsonValueKind.Undefined ? -lostDeposits.RequiredInt64("change") : 0,
                OffenderLostReward = lostRewards.ValueKind != JsonValueKind.Undefined ? -lostRewards.RequiredInt64("change") : 0,
                OffenderLostFee = lostFees.ValueKind != JsonValueKind.Undefined ? -lostFees.RequiredInt64("change") : 0,
            };
            #endregion

            #region entities
            //var block = doubleBaking.Block;
            var accuser = doubleBaking.Accuser;
            var offender = doubleBaking.Offender;

            //Db.TryAttach(block);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance += doubleBaking.AccuserReward;

            offender.Balance -= doubleBaking.OffenderLostDeposit;
            offender.StakingBalance -= doubleBaking.OffenderLostDeposit;

            offender.Balance -= doubleBaking.OffenderLostReward;

            offender.Balance -= doubleBaking.OffenderLostFee;
            offender.StakingBalance -= doubleBaking.OffenderLostFee;

            accuser.DoubleBakingCount++;
            if (offender != accuser) offender.DoubleBakingCount++;

            block.Operations |= Operations.DoubleBakings;
            #endregion

            Db.DoubleBakingOps.Add(doubleBaking);
            return Task.CompletedTask;
        }

        public virtual Task Revert(Block block, DoubleBakingOperation doubleBaking)
        {
            #region init
            doubleBaking.Block ??= block;
            doubleBaking.Block.Baker ??= Cache.Accounts.GetDelegate(block.BakerId);

            doubleBaking.Accuser ??= Cache.Accounts.GetDelegate(doubleBaking.AccuserId);
            doubleBaking.Offender ??= Cache.Accounts.GetDelegate(doubleBaking.OffenderId);
            #endregion

            #region entities
            //var block = doubleBaking.Block;
            var accuser = doubleBaking.Accuser;
            var offender = doubleBaking.Offender;

            //Db.TryAttach(block);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance -= doubleBaking.AccuserReward;

            offender.Balance += doubleBaking.OffenderLostDeposit;
            offender.StakingBalance += doubleBaking.OffenderLostDeposit;

            offender.Balance += doubleBaking.OffenderLostReward;

            offender.Balance += doubleBaking.OffenderLostFee;
            offender.StakingBalance += doubleBaking.OffenderLostFee;

            accuser.DoubleBakingCount--;
            if (offender != accuser) offender.DoubleBakingCount--;
            #endregion

            Db.DoubleBakingOps.Remove(doubleBaking);
            return Task.CompletedTask;
        }
    }
}
