using System.Text.Json;
using Netmavryk.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class NonceRevelationsCommit : ProtocolCommit
    {
        public NonceRevelationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var balanceUpdates = content
                .Required("metadata")
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .ToList();

            var (rewardDelegated, rewardStakedOwn, rewardStakedEdge, rewardStakedShared) = ParseRewards(block.Proposer, balanceUpdates);

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
                RewardDelegated = rewardDelegated,
                RewardStakedOwn = rewardStakedOwn,
                RewardStakedEdge = rewardStakedEdge,
                RewardStakedShared = rewardStakedShared
            };
            #endregion

            #region apply operation
            block.Proposer.Balance += revelation.RewardDelegated + revelation.RewardStakedOwn + revelation.RewardStakedEdge;
            block.Proposer.StakingBalance += revelation.RewardDelegated + revelation.RewardStakedOwn + revelation.RewardStakedEdge + revelation.RewardStakedShared;
            block.Proposer.OwnStakedBalance += revelation.RewardStakedOwn + revelation.RewardStakedEdge;
            block.Proposer.ExternalStakedBalance += revelation.RewardStakedShared;
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

            Cache.Statistics.Current.TotalCreated += revelation.RewardDelegated + revelation.RewardStakedOwn + revelation.RewardStakedEdge + revelation.RewardStakedShared;
            Cache.Statistics.Current.TotalFrozen += revelation.RewardStakedOwn + revelation.RewardStakedEdge + revelation.RewardStakedShared;
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
            block.Proposer.Balance -= revelation.RewardDelegated + revelation.RewardStakedOwn + revelation.RewardStakedEdge;
            block.Proposer.StakingBalance -= revelation.RewardDelegated + revelation.RewardStakedOwn + revelation.RewardStakedEdge + revelation.RewardStakedShared;
            block.Proposer.OwnStakedBalance -= revelation.RewardStakedOwn + revelation.RewardStakedEdge;
            block.Proposer.ExternalStakedBalance -= revelation.RewardStakedShared;
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

        protected virtual (long, long, long, long) ParseRewards(Data.Models.Delegate proposer, List<JsonElement> balanceUpdates)
        {
            var freezerUpdate = balanceUpdates.SingleOrDefault(x => x.RequiredString("kind") == "freezer");
            var contractUpdate = balanceUpdates.SingleOrDefault(x => x.RequiredString("kind") == "contract");

            var rewardDelegated = contractUpdate.ValueKind != JsonValueKind.Undefined
                ? contractUpdate.RequiredInt64("change")
                : 0;
            var rewardStakedOwn = freezerUpdate.ValueKind != JsonValueKind.Undefined
                ? freezerUpdate.RequiredInt64("change")
                : 0;

            return (rewardDelegated, rewardStakedOwn, 0L, 0L);
        }
    }
}
