using System.Text.Json;
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
            var lostDepositsValue = lostDeposits.ValueKind != JsonValueKind.Undefined ? -lostDeposits.RequiredInt64("change") : 0;

            var lostRewards = balanceUpdates
                .FirstOrDefault(x => x.RequiredString("category")[0] == 'r' && x.RequiredInt64("change") < 0);
            var lostRewardsValue = lostRewards.ValueKind != JsonValueKind.Undefined ? -lostRewards.RequiredInt64("change") : 0;

            var lostFees = balanceUpdates
                .FirstOrDefault(x => x.RequiredString("category")[0] == 'f' && x.RequiredInt64("change") < 0);
            var lostFeesValue = lostFees.ValueKind != JsonValueKind.Undefined ? -lostFees.RequiredInt64("change") : 0;

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
            ReceiveLockedRewards(accuser, doubleBaking.Reward);
            Spend(offender, offender, lostDepositsValue + lostFeesValue);
            BurnLockedRewards(offender, lostRewardsValue);

            accuser.DoubleBakingCount++;
            if (offender != accuser) offender.DoubleBakingCount++;

            block.Operations |= Operations.DoubleBakings;

            Cache.AppState.Get().DoubleBakingOpsCount++;
            Cache.Statistics.Current.TotalBurned += doubleBaking.LostStaked - doubleBaking.Reward;
            Cache.Statistics.Current.TotalFrozen -= doubleBaking.LostStaked - doubleBaking.Reward;
            #endregion

            Db.DoubleBakingOps.Add(doubleBaking);
            Context.DoubleBakingOps.Add(doubleBaking);
            return Task.CompletedTask;
        }

        public virtual Task Revert(Block block, DoubleBakingOperation doubleBaking)
        {
            #region entities
            //var block = doubleBaking.Block;
            var accuser = Cache.Accounts.GetDelegate(doubleBaking.AccuserId);
            var offender = Cache.Accounts.GetDelegate(doubleBaking.OffenderId);

            //Db.TryAttach(block);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            RevertReceiveLockedRewards(accuser, doubleBaking.Reward);
            // here we can miss 1 mutez, but this may happen only in legacy protocols
            // TODO: replace it with NotImplementedException after Ithaca
            RevertSpend(offender, offender, doubleBaking.Reward * 2);
            RevertBurnLockedRewards(offender, doubleBaking.LostStaked - doubleBaking.Reward * 2);

            accuser.DoubleBakingCount--;
            if (offender != accuser) offender.DoubleBakingCount--;

            Cache.AppState.Get().DoubleBakingOpsCount--;
            #endregion

            Db.DoubleBakingOps.Remove(doubleBaking);
            Cache.AppState.ReleaseOperationId();
            return Task.CompletedTask;
        }
    }
}
