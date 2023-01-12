using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto4
{
    class DoubleEndorsingCommit : ProtocolCommit
    {
        public DoubleEndorsingCommit(ProtocolHandler protocol) : base(protocol) { }

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
            var lostDepositsValue = lostDeposits.ValueKind != JsonValueKind.Undefined ? -lostDeposits.RequiredInt64("change") : 0;

            var lostRewards = balanceUpdates
                .FirstOrDefault(x => x.RequiredString("category")[0] == 'r' && x.RequiredInt64("change") < 0);
            var lostRewardsValue = lostRewards.ValueKind != JsonValueKind.Undefined ? -lostRewards.RequiredInt64("change") : 0;

            var lostFees = balanceUpdates
                .FirstOrDefault(x => x.RequiredString("category")[0] == 'f' && x.RequiredInt64("change") < 0);
            var lostFeesValue = lostFees.ValueKind != JsonValueKind.Undefined ? -lostFees.RequiredInt64("change") : 0;

            var doubleEndorsing = new DoubleEndorsingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                AccusedLevel = content.Required("op1").Required("operations").RequiredInt32("level"),
                Accuser = block.Proposer,
                Offender = Cache.Accounts.GetDelegate(offenderAddr),

                AccuserReward = rewards.ValueKind != JsonValueKind.Undefined ? rewards.RequiredInt64("change") : 0,
                OffenderLoss = lostDepositsValue + lostRewardsValue + lostFeesValue
            };
            #endregion

            #region entities
            //var block = doubleEndorsing.Block;
            var accuser = doubleEndorsing.Accuser;
            var offender = doubleEndorsing.Offender;

            //Db.TryAttach(block);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance += doubleEndorsing.AccuserReward;
            offender.Balance -= doubleEndorsing.OffenderLoss;
            offender.StakingBalance -= lostDepositsValue;
            offender.StakingBalance -= lostFeesValue;

            accuser.DoubleEndorsingCount++;
            if (offender != accuser) offender.DoubleEndorsingCount++;

            block.Operations |= Operations.DoubleEndorsings;
            #endregion

            Db.DoubleEndorsingOps.Add(doubleEndorsing);
            return Task.CompletedTask;
        }

        public virtual Task Revert(Block block, DoubleEndorsingOperation doubleEndorsing)
        {
            #region init
            doubleEndorsing.Block ??= block;
            doubleEndorsing.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            doubleEndorsing.Accuser ??= Cache.Accounts.GetDelegate(doubleEndorsing.AccuserId);
            doubleEndorsing.Offender ??= Cache.Accounts.GetDelegate(doubleEndorsing.OffenderId);
            #endregion

            #region entities
            //var block = doubleEndorsing.Block;
            var accuser = doubleEndorsing.Accuser;
            var offender = doubleEndorsing.Offender;

            //Db.TryAttach(block);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance -= doubleEndorsing.AccuserReward;
            offender.Balance += doubleEndorsing.OffenderLoss;
            offender.StakingBalance += doubleEndorsing.AccuserReward * 2;
            // here we can miss 1 mutez, but this may happen only in legacy protocols, so let's ignore
            // TODO: replace it with NotImplementedException after Ithaca

            accuser.DoubleEndorsingCount--;
            if (offender != accuser) offender.DoubleEndorsingCount--;
            #endregion

            Db.DoubleEndorsingOps.Remove(doubleEndorsing);
            Cache.AppState.ReleaseOperationId();
            return Task.CompletedTask;
        }
    }
}
