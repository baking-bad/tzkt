using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto4
{
    class DoubleAttestationCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
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

            var doubleAttestation = new DoubleAttestationOperation
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
            accuser.Balance += doubleAttestation.Reward;
            offender.Balance -= doubleAttestation.LostStaked;
            offender.StakingBalance -= lostDepositsValue;
            offender.StakingBalance -= lostFeesValue;

            accuser.DoubleAttestationCount++;
            if (offender != accuser) offender.DoubleAttestationCount++;

            block.Operations |= Operations.DoubleAttestations;

            Cache.AppState.Get().DoubleAttestationOpsCount++;
            Cache.Statistics.Current.TotalBurned += doubleAttestation.LostStaked - doubleAttestation.Reward;
            Cache.Statistics.Current.TotalFrozen -= doubleAttestation.LostStaked - doubleAttestation.Reward;
            #endregion

            Db.DoubleAttestationOps.Add(doubleAttestation);
            Context.DoubleAttestationOps.Add(doubleAttestation);
            return Task.CompletedTask;
        }

        public virtual Task Revert(Block block, DoubleAttestationOperation doubleAttestation)
        {
            #region entities
            //var block = doubleAttestation.Block;
            var accuser = Cache.Accounts.GetDelegate(doubleAttestation.AccuserId);
            var offender = Cache.Accounts.GetDelegate(doubleAttestation.OffenderId);

            //Db.TryAttach(block);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance -= doubleAttestation.Reward;
            offender.Balance += doubleAttestation.LostStaked;
            offender.StakingBalance += doubleAttestation.Reward * 2;
            // here we can miss 1 mutez, but this may happen only in legacy protocols, so let's ignore
            // TODO: replace it with NotImplementedException after Ithaca

            accuser.DoubleAttestationCount--;
            if (offender != accuser) offender.DoubleAttestationCount--;

            Cache.AppState.Get().DoubleAttestationOpsCount--;
            #endregion

            Db.DoubleAttestationOps.Remove(doubleAttestation);
            Cache.AppState.ReleaseOperationId();
            return Task.CompletedTask;
        }
    }
}
