using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class DoubleEndorsingCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public void Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var accusedLevel = GetAccusedLevel(content);
            var accuser = Context.Proposer;
            var offender = Cache.Accounts.GetExistingDelegate(GetOffender(content));

            var operation = new DoubleEndorsingOperation
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
            accuser.DoubleEndorsingCount++;

            if (offender != accuser)
            {
                Db.TryAttach(offender);
                offender.DoubleEndorsingCount++;
            }

            block.Operations |= Operations.DoubleEndorsings;

            Cache.AppState.Get().DoubleEndorsingOpsCount++;
            #endregion

            Db.DoubleEndorsingOps.Add(operation);
            Context.DoubleEndorsingOps.Add(operation);
        }

        public void Revert(DoubleEndorsingOperation operation)
        {
            #region init
            var accuser = Cache.Accounts.GetDelegate(operation.AccuserId);
            var offender = Cache.Accounts.GetDelegate(operation.OffenderId);
            #endregion

            #region revert operation
            Db.TryAttach(accuser);
            accuser.DoubleEndorsingCount--;

            if (offender != accuser)
            {
                Db.TryAttach(offender);
                offender.DoubleEndorsingCount--;
            }

            Cache.AppState.Get().DoubleEndorsingOpsCount--;
            #endregion

            Db.DoubleEndorsingOps.Remove(operation);
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
