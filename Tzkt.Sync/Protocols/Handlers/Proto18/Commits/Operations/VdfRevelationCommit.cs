﻿using System.Numerics;
using System.Text.Json;
using Netmavryk.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class VdfRevelationCommit : ProtocolCommit
    {
        public VdfRevelationCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var balanceUpdates = content.Required("metadata").RequiredArray("balance_updates").EnumerateArray();
            var freezerUpdate = balanceUpdates.SingleOrDefault(x => x.RequiredString("kind") == "freezer");
            var contractUpdate = balanceUpdates.SingleOrDefault(x => x.RequiredString("kind") == "contract");

            var rewardLiquid = contractUpdate.ValueKind != JsonValueKind.Undefined
                ? contractUpdate.RequiredInt64("change")
                : 0;
            var rewardStaked = freezerUpdate.ValueKind != JsonValueKind.Undefined
                ? freezerUpdate.RequiredInt64("change")
                : 0;
            var rewardStakedOwn = block.Proposer.TotalStakedBalance == 0 ? rewardStaked : (long)((BigInteger)rewardStaked * block.Proposer.StakedBalance / block.Proposer.TotalStakedBalance);
            var rewardStakedShared = rewardStaked - rewardStakedOwn;

            var revelation = new VdfRevelationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                Baker = block.Proposer,
                Cycle = block.Cycle,
                Solution = Hex.Parse(content.RequiredArray("solution", 2)[0].RequiredString()),
                Proof = Hex.Parse(content.RequiredArray("solution", 2)[1].RequiredString()),
                RewardLiquid = rewardLiquid,
                RewardStakedOwn = rewardStakedOwn,
                RewardStakedShared = rewardStakedShared
            };
            #endregion

            #region apply operation
            block.Proposer.Balance += revelation.RewardLiquid + revelation.RewardStakedOwn;
            block.Proposer.StakingBalance += revelation.RewardLiquid + revelation.RewardStakedOwn + revelation.RewardStakedShared;
            block.Proposer.StakedBalance += revelation.RewardStakedOwn;
            block.Proposer.ExternalStakedBalance += revelation.RewardStakedShared;
            block.Proposer.TotalStakedBalance += revelation.RewardStakedOwn + revelation.RewardStakedShared;
            block.Proposer.VdfRevelationsCount++;

            Cache.AppState.Get().VdfRevelationOpsCount++;

            block.Operations |= Operations.VdfRevelation;

            Cache.Statistics.Current.TotalCreated += revelation.RewardLiquid + revelation.RewardStakedOwn + revelation.RewardStakedShared;
            Cache.Statistics.Current.TotalFrozen += revelation.RewardStakedOwn + revelation.RewardStakedShared;
            #endregion

            Db.VdfRevelationOps.Add(revelation);
            return Task.CompletedTask;
        }

        public virtual Task Revert(Block block, VdfRevelationOperation revelation)
        {
            #region init
            revelation.Baker ??= Cache.Accounts.GetDelegate(revelation.BakerId);
            #endregion

            #region entities
            var blockBaker = revelation.Baker;
            Db.TryAttach(blockBaker);
            #endregion

            #region apply operation
            block.Proposer.Balance -= revelation.RewardLiquid + revelation.RewardStakedOwn;
            block.Proposer.StakingBalance -= revelation.RewardLiquid + revelation.RewardStakedOwn + revelation.RewardStakedShared;
            block.Proposer.StakedBalance -= revelation.RewardStakedOwn;
            block.Proposer.ExternalStakedBalance -= revelation.RewardStakedShared;
            block.Proposer.TotalStakedBalance -= revelation.RewardStakedOwn + revelation.RewardStakedShared;
            block.Proposer.VdfRevelationsCount--;

            Cache.AppState.Get().VdfRevelationOpsCount--;
            #endregion

            Db.VdfRevelationOps.Remove(revelation);
            Cache.AppState.ReleaseOperationId();
            return Task.CompletedTask;
        }
    }
}
