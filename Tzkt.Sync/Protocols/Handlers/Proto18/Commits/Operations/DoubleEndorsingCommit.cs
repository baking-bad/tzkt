using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class DoubleEndorsingCommit : ProtocolCommit
    {
        public DoubleEndorsingCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var accusedLevel = content.Required("op1").Required("operations").RequiredInt32("level") + 1;
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

            var operation = new DoubleEndorsingOperation
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
            accuser.DoubleEndorsingCount++;

            if (offender != accuser)
            {
                Db.TryAttach(offender);
                offender.DoubleEndorsingCount++;
            }

            block.Operations |= Operations.DoubleEndorsings;
            #endregion

            Db.DoubleEndorsingOps.Add(operation);
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
            #endregion

            Db.DoubleEndorsingOps.Remove(operation);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
