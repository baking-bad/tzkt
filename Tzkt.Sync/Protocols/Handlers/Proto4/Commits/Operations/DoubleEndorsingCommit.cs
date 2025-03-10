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

            var accuser = Context.Proposer;
            var offender = Cache.Accounts.GetDelegate(offenderAddr);

            var doubleEndorsing = new DoubleEndorsingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                SlashedLevel = block.Level,
                AccusedLevel = content.Required("op1").Required("operations").RequiredInt32("level"),
                AccuserId = accuser.Id,
                OffenderId = offender.Id,

                Reward = rewards.ValueKind != JsonValueKind.Undefined ? rewards.RequiredInt64("change") : 0,
                LostStaked = lostDepositsValue + lostRewardsValue + lostFeesValue,
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
            offender.Balance -= doubleEndorsing.LostStaked;
            offender.StakingBalance -= lostDepositsValue;
            offender.StakingBalance -= lostFeesValue;

            accuser.DoubleEndorsingCount++;
            if (offender != accuser) offender.DoubleEndorsingCount++;

            block.Operations |= Operations.DoubleEndorsings;

            Cache.AppState.Get().DoubleEndorsingOpsCount++;
            Cache.Statistics.Current.TotalBurned += doubleEndorsing.LostStaked - doubleEndorsing.Reward;
            Cache.Statistics.Current.TotalFrozen -= doubleEndorsing.LostStaked - doubleEndorsing.Reward;
            #endregion

            Db.DoubleEndorsingOps.Add(doubleEndorsing);
            Context.DoubleEndorsingOps.Add(doubleEndorsing);
            return Task.CompletedTask;
        }

        public virtual Task Revert(Block block, DoubleEndorsingOperation doubleEndorsing)
        {
            #region entities
            //var block = doubleEndorsing.Block;
            var accuser = Cache.Accounts.GetDelegate(doubleEndorsing.AccuserId);
            var offender = Cache.Accounts.GetDelegate(doubleEndorsing.OffenderId);

            //Db.TryAttach(block);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance -= doubleEndorsing.Reward;
            offender.Balance += doubleEndorsing.LostStaked;
            offender.StakingBalance += doubleEndorsing.Reward * 2;
            // here we can miss 1 mutez, but this may happen only in legacy protocols, so let's ignore
            // TODO: replace it with NotImplementedException after Ithaca

            accuser.DoubleEndorsingCount--;
            if (offender != accuser) offender.DoubleEndorsingCount--;

            Cache.AppState.Get().DoubleEndorsingOpsCount--;
            #endregion

            Db.DoubleEndorsingOps.Remove(doubleEndorsing);
            Cache.AppState.ReleaseOperationId();
            return Task.CompletedTask;
        }
    }
}
