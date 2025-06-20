﻿using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class DoubleBakingCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var accusedLevel = content.Required("bh1").RequiredInt32("level");
            var accusedRound = Hex.Parse(content.Required("bh1").RequiredArray("fitness", 5)[4].RequiredString()).ToInt32();
            var accusedBakerId = (await Db.BakingRights.AsNoTracking().FirstOrDefaultAsync(x => x.Level == accusedLevel && x.Round == accusedRound))?.BakerId;
            if (accusedBakerId == null)
            {
                var rpcRights = await Proto.Rpc.GetLevelBakingRightsAsync(block.Level, accusedLevel, accusedRound);
                var accusedBaker = rpcRights
                    .EnumerateArray()
                    .First(x => x.RequiredInt32("level") == accusedLevel && x.RequiredInt32("round") == accusedRound)
                    .RequiredString("delegate");
                accusedBakerId = Cache.Accounts.GetExistingDelegate(accusedBaker).Id;
            }

            var accuser = Context.Proposer;
            var offender = Cache.Accounts.GetDelegate(accusedBakerId.Value);

            var operation = new DoubleBakingOperation
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
            accuser.DoubleBakingCount++;

            if (offender != accuser)
            {
                Db.TryAttach(offender);
                offender.DoubleBakingCount++;
            }

            block.Operations |= Operations.DoubleBakings;

            Cache.AppState.Get().DoubleBakingOpsCount++;
            #endregion

            Db.DoubleBakingOps.Add(operation);
            Context.DoubleBakingOps.Add(operation);
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

            Cache.AppState.Get().DoubleBakingOpsCount--;
            #endregion

            Db.DoubleBakingOps.Remove(operation);
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual int GetSlashingLevel(Block block, Protocol protocol, int accusedLevel)
        {
            return Cache.Protocols.GetCycleEnd(block.Cycle);
        }
    }
}
