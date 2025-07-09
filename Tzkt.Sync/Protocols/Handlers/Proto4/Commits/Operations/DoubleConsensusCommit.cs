using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto4
{
    class DoubleConsensusCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
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

            var doubleConsensus = new DoubleConsensusOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                Kind = DoubleConsensusKind.DoubleAttestation,

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
            accuser.Balance += doubleConsensus.Reward;
            offender.Balance -= doubleConsensus.LostStaked;
            offender.StakingBalance -= lostDepositsValue;
            offender.StakingBalance -= lostFeesValue;

            accuser.DoubleConsensusCount++;
            if (offender != accuser) offender.DoubleConsensusCount++;

            block.Operations |= Operations.DoubleConsensus;

            Cache.AppState.Get().DoubleConsensusOpsCount++;
            Cache.Statistics.Current.TotalBurned += doubleConsensus.LostStaked - doubleConsensus.Reward;
            Cache.Statistics.Current.TotalFrozen -= doubleConsensus.LostStaked - doubleConsensus.Reward;
            #endregion

            Db.DoubleConsensusOps.Add(doubleConsensus);
            Context.DoubleConsensusOps.Add(doubleConsensus);
            return Task.CompletedTask;
        }

        public virtual Task Revert(Block block, DoubleConsensusOperation doubleConsensus)
        {
            #region entities
            //var block = doubleConsensus.Block;
            var accuser = Cache.Accounts.GetDelegate(doubleConsensus.AccuserId);
            var offender = Cache.Accounts.GetDelegate(doubleConsensus.OffenderId);

            //Db.TryAttach(block);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance -= doubleConsensus.Reward;
            offender.Balance += doubleConsensus.LostStaked;
            offender.StakingBalance += doubleConsensus.Reward * 2;
            // here we can miss 1 mutez, but this may happen only in legacy protocols, so let's ignore
            // TODO: replace it with NotImplementedException after Ithaca

            accuser.DoubleConsensusCount--;
            if (offender != accuser) offender.DoubleConsensusCount--;

            Cache.AppState.Get().DoubleConsensusOpsCount--;
            #endregion

            Db.DoubleConsensusOps.Remove(doubleConsensus);
            Cache.AppState.ReleaseOperationId();
            return Task.CompletedTask;
        }
    }
}
