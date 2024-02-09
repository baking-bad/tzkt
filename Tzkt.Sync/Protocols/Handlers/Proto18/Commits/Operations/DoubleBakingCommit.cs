using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netmavryk.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class DoubleBakingCommit : ProtocolCommit
    {
        public DoubleBakingCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var accusedLevel = content.Required("bh1").RequiredInt32("level");
            var accusedRound = Hex.Parse(content.Required("bh1").RequiredArray("fitness", 5)[4].RequiredString()).ToInt32();
            var accusedRight = await Db.BakingRights.FirstAsync(x => x.Level == accusedLevel && x.Round == accusedRound);

            var accuser = block.Proposer;
            var offender = Cache.Accounts.GetDelegate(accusedRight.BakerId);

            var operation = new DoubleBakingOperation
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

                Reward = 0,
                LostStaked = 0,
                LostUnstaked = 0,
                LostExternalStaked = 0,
                LostExternalUnstaked = 0,

                RoundingLoss = 0
            };
            #endregion

            #region apply operation
            Db.TryAttach(accuser);
            accuser.DoubleBakingCount++;

            if (offender != accuser)
            {
                Db.TryAttach(offender);
                offender.DoubleBakingCount++;
            }

            block.Operations |= Operations.DoubleBakings;
            #endregion

            Db.DoubleBakingOps.Add(operation);
        }

        public void Revert(DoubleBakingOperation operation)
        {
            #region init
            var accuser = Cache.Accounts.GetDelegate(operation.AccuserId);
            var offender = Cache.Accounts.GetDelegate(operation.OffenderId);
            #endregion

            #region revert operation
            Db.TryAttach(accuser);
            accuser.DoubleBakingCount--;

            if (offender != accuser)
            {
                Db.TryAttach(offender);
                offender.DoubleBakingCount--;
            }
            #endregion

            Db.DoubleBakingOps.Remove(operation);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
