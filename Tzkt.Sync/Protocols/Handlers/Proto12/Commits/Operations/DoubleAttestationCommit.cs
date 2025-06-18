using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class DoubleAttestationCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual void Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var balanceUpdates = content.Required("metadata").RequiredArray("balance_updates").EnumerateArray();
            var freezerUpdates = balanceUpdates.Where(x => x.RequiredString("kind") == "freezer" && x.RequiredString("category") == "deposits");
            var contractUpdates = balanceUpdates.Where(x => x.RequiredString("kind") == "contract");

            var offenderAddr = freezerUpdates.Any()
                ? freezerUpdates.First().RequiredString("delegate")
                : Context.Proposer.Address; // this is wrong, but no big deal

            var offenderLoss = freezerUpdates.Any()
                ? -freezerUpdates.Sum(x => x.RequiredInt64("change"))
                : 0;

            var accuserReward = contractUpdates.Any()
                ? contractUpdates.Sum(x => x.RequiredInt64("change"))
                : 0;

            var accuser = Context.Proposer;
            var offender = Cache.Accounts.GetExistingDelegate(offenderAddr);

            var doubleAttestation = new DoubleAttestationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                SlashedLevel = block.Level,
                AccusedLevel = content.Required("op1").Required("operations").RequiredInt32("level") + 1,
                AccuserId = accuser.Id,
                OffenderId = offender.Id,

                Reward = accuserReward,
                LostStaked = offenderLoss,
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
            accuser.StakingBalance += doubleAttestation.Reward;

            offender.Balance -= doubleAttestation.LostStaked;
            offender.StakingBalance -= doubleAttestation.LostStaked;

            accuser.DoubleAttestationCount++;
            if (offender != accuser) offender.DoubleAttestationCount++;

            block.Operations |= Operations.DoubleAttestations;

            Cache.AppState.Get().DoubleAttestationOpsCount++;
            Cache.Statistics.Current.TotalBurned += doubleAttestation.LostStaked - doubleAttestation.Reward;
            Cache.Statistics.Current.TotalFrozen -= doubleAttestation.LostStaked;
            #endregion

            Db.DoubleAttestationOps.Add(doubleAttestation);
            Context.DoubleAttestationOps.Add(doubleAttestation);
        }

        public virtual void Revert(Block block, DoubleAttestationOperation doubleAttestation)
        {
            #region entities
            var accuser = Cache.Accounts.GetDelegate(doubleAttestation.AccuserId);
            var offender = Cache.Accounts.GetDelegate(doubleAttestation.OffenderId);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance -= doubleAttestation.Reward;
            accuser.StakingBalance -= doubleAttestation.Reward;

            offender.Balance += doubleAttestation.LostStaked;
            offender.StakingBalance += doubleAttestation.LostStaked;

            accuser.DoubleAttestationCount--;
            if (offender != accuser) offender.DoubleAttestationCount--;

            Cache.AppState.Get().DoubleAttestationOpsCount--;
            #endregion

            Db.DoubleAttestationOps.Remove(doubleAttestation);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
