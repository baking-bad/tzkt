using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class DoublePreendorsingCommit : ProtocolCommit
    {
        public DoublePreendorsingCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var accusedLevel = content.Required("op1").Required("operations").RequiredInt32("level");
            var sig1 = content.Required("op1").RequiredString("signature");
            var sig2 = content.Required("op2").RequiredString("signature");
            var accusedBlock = await Proto.Rpc.GetBlockAsync(accusedLevel);
            var accusedOp = accusedBlock.Required("operations")[0].EnumerateArray().First(x =>
            {
                var sig = x.RequiredString("signature");
                return sig == sig1 || sig == sig2;
            });

            var accuser = block.Proposer;
            var offender = Cache.Accounts.GetDelegate(accusedOp.Required("contents")[0].Required("metadata").RequiredString("delegate"));

            var operation = new DoublePreendorsingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                AccusedLevel = accusedLevel,
                SlashedLevel = block.Protocol.GetCycleEnd(block.Cycle),

                Accuser = accuser,
                Offender = offender,

                AccuserReward = 0,
                OffenderLossOwn = 0,
                OffenderLossShared = 0
            };
            #endregion

            #region apply operation
            Db.TryAttach(accuser);
            accuser.DoublePreendorsingCount++;

            if (offender != accuser)
            {
                Db.TryAttach(offender);
                offender.DoublePreendorsingCount++;
            }

            block.Operations |= Operations.DoublePreendorsings;
            #endregion

            Db.DoublePreendorsingOps.Add(operation);
        }

        public void Revert(DoublePreendorsingOperation operation)
        {
            #region init
            var accuser = Cache.Accounts.GetDelegate(operation.AccuserId);
            var offender = Cache.Accounts.GetDelegate(operation.OffenderId);
            #endregion

            #region revert operation
            Db.TryAttach(accuser);
            accuser.DoublePreendorsingCount--;

            if (offender != accuser)
            {
                Db.TryAttach(offender);
                offender.DoublePreendorsingCount--;
            }
            #endregion

            Db.DoublePreendorsingOps.Remove(operation);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
