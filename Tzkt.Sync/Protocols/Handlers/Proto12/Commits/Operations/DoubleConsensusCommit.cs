using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class DoubleConsensusCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
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

            var kind = content.RequiredString("kind") == "double_endorsement_evidence"
                ? DoubleConsensusKind.DoubleAttestation
                : DoubleConsensusKind.DoublePreattestation;

            var doubleConsensus = new DoubleConsensusOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                Kind = kind,

                SlashedLevel = block.Level,
                AccusedLevel = content.Required("op1").Required("operations").RequiredInt32("level")
                    + (kind == DoubleConsensusKind.DoubleAttestation ? 1 : 0),

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
            Receive(accuser, accuser, doubleConsensus.Reward);

            Spend(offender, offender, doubleConsensus.LostStaked);

            accuser.DoubleConsensusCount++;
            if (offender != accuser) offender.DoubleConsensusCount++;

            block.Operations |= Operations.DoubleConsensus;

            Cache.AppState.Get().DoubleConsensusOpsCount++;
            Cache.Statistics.Current.TotalBurned += doubleConsensus.LostStaked - doubleConsensus.Reward;
            Cache.Statistics.Current.TotalFrozen -= doubleConsensus.LostStaked;
            #endregion

            Db.DoubleConsensusOps.Add(doubleConsensus);
            Context.DoubleConsensusOps.Add(doubleConsensus);
        }

        public virtual void Revert(Block block, DoubleConsensusOperation doubleConsensus)
        {
            #region entities
            var accuser = Cache.Accounts.GetDelegate(doubleConsensus.AccuserId);
            var offender = Cache.Accounts.GetDelegate(doubleConsensus.OffenderId);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            RevertReceive(accuser, accuser, doubleConsensus.Reward);

            RevertSpend(offender, offender, doubleConsensus.LostStaked);

            accuser.DoubleConsensusCount--;
            if (offender != accuser) offender.DoubleConsensusCount--;

            Cache.AppState.Get().DoubleConsensusOpsCount--;
            #endregion

            Db.DoubleConsensusOps.Remove(doubleConsensus);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
