﻿using System.Numerics;
using System.Text.Json;
using Netmavryk.Encoding;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto18
{
    class NonceRevelationsCommit : ProtocolCommit
    {
        public NonceRevelationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
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

            var revealedBlock = await Cache.Blocks.GetAsync(content.RequiredInt32("level"));

            var revelation = new NonceRevelationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                Baker = block.Proposer,
                Sender = Cache.Accounts.GetDelegate(revealedBlock.ProposerId),
                RevealedBlock = revealedBlock,
                RevealedLevel = revealedBlock.Level,
                RevealedCycle = revealedBlock.Cycle,
                Nonce = Hex.Parse(content.RequiredString("nonce")),
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
            block.Proposer.NonceRevelationsCount++;

            if (revelation.Sender != block.Proposer)
            {
                Db.TryAttach(revelation.Sender);
                revelation.Sender.NonceRevelationsCount++;
            }

            Db.TryAttach(revelation.RevealedBlock);
            revelation.RevealedBlock.Revelation = revelation;
            revelation.RevealedBlock.RevelationId = revelation.Id;

            block.Operations |= Operations.Revelations;

            Cache.Statistics.Current.TotalCreated += revelation.RewardLiquid + revelation.RewardStakedOwn + revelation.RewardStakedShared;
            Cache.Statistics.Current.TotalFrozen += revelation.RewardStakedOwn + revelation.RewardStakedShared;
            #endregion

            Db.NonceRevelationOps.Add(revelation);
        }

        public virtual async Task Revert(Block block, NonceRevelationOperation revelation)
        {
            #region init
            revelation.Block ??= block;
            revelation.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            revelation.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            revelation.Baker ??= Cache.Accounts.GetDelegate(revelation.BakerId);
            revelation.Sender ??= Cache.Accounts.GetDelegate(revelation.SenderId);
            revelation.RevealedBlock = await Cache.Blocks.GetAsync(revelation.RevealedLevel);
            #endregion

            #region apply operation
            Db.TryAttach(block.Proposer);
            block.Proposer.Balance -= revelation.RewardLiquid + revelation.RewardStakedOwn;
            block.Proposer.StakingBalance -= revelation.RewardLiquid + revelation.RewardStakedOwn + revelation.RewardStakedShared;
            block.Proposer.StakedBalance -= revelation.RewardStakedOwn;
            block.Proposer.ExternalStakedBalance -= revelation.RewardStakedShared;
            block.Proposer.TotalStakedBalance -= revelation.RewardStakedOwn + revelation.RewardStakedShared;
            block.Proposer.NonceRevelationsCount--;

            if (revelation.Sender != block.Proposer)
            {
                Db.TryAttach(revelation.Sender);
                revelation.Sender.NonceRevelationsCount--;
            }

            Db.TryAttach(revelation.RevealedBlock);
            revelation.RevealedBlock.Revelation = null;
            revelation.RevealedBlock.RevelationId = null;
            #endregion

            Db.NonceRevelationOps.Remove(revelation);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
