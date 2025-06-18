using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class DoubleAttestationCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public void Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var accusedLevel = GetAccusedLevel(content);
            var accuser = Context.Proposer;
            var offender = Cache.Accounts.GetExistingDelegate(GetOffender(content));

            var operation = new DoubleAttestationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                AccusedLevel = accusedLevel,
                SlashedLevel = GetSlashingLevel(block, Context.Protocol, accusedLevel),

                AccuserId = accuser.Id,
                OffenderId = offender.Id,

                Reward = 0,
                LostStaked = 0,
                LostUnstaked = 0,
                LostExternalStaked = 0,
                LostExternalUnstaked = 0
            };
            #endregion

            #region apply operation
            Db.TryAttach(accuser);
            accuser.DoubleAttestationCount++;

            if (offender != accuser)
            {
                Db.TryAttach(offender);
                offender.DoubleAttestationCount++;
            }

            block.Operations |= Operations.DoubleAttestations;

            Cache.AppState.Get().DoubleAttestationOpsCount++;
            #endregion

            Db.DoubleAttestationOps.Add(operation);
            Context.DoubleAttestationOps.Add(operation);
        }

        public void Revert(DoubleAttestationOperation operation)
        {
            #region init
            var accuser = Cache.Accounts.GetDelegate(operation.AccuserId);
            var offender = Cache.Accounts.GetDelegate(operation.OffenderId);
            #endregion

            #region revert operation
            Db.TryAttach(accuser);
            accuser.DoubleAttestationCount--;

            if (offender != accuser)
            {
                Db.TryAttach(offender);
                offender.DoubleAttestationCount--;
            }

            Cache.AppState.Get().DoubleAttestationOpsCount--;
            #endregion

            Db.DoubleAttestationOps.Remove(operation);
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual int GetAccusedLevel(JsonElement content)
        {
            return content.Required("op1").Required("operations").RequiredInt32("level");
        }

        protected virtual string GetOffender(JsonElement content)
        {
            return content.Required("metadata").RequiredString("forbidden_delegate");
        }

        protected virtual int GetSlashingLevel(Block block, Protocol protocol, int accusedLevel)
        {
            return Cache.Protocols.GetCycleEnd(block.Cycle);
        }
    }
}
