using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class NonceRevelationsCommit : ProtocolCommit
    {
        public NonceRevelationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var baker = Context.Proposer;

            var balanceUpdates = content
                .Required("metadata")
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .ToList();

            var (rewardDelegated, rewardStakedOwn, rewardStakedEdge, rewardStakedShared) = ParseRewards(Context.Proposer, balanceUpdates);

            var revealedBlock = await Cache.Blocks.GetAsync(content.RequiredInt32("level"));
            var sender = Cache.Accounts.GetDelegate(revealedBlock.ProposerId);

            var revelation = new NonceRevelationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                BakerId = baker.Id,
                SenderId = sender.Id,
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
            baker.Balance += revelation.RewardDelegated + revelation.RewardStakedOwn + revelation.RewardStakedEdge;
            baker.StakingBalance += revelation.RewardDelegated + revelation.RewardStakedOwn + revelation.RewardStakedEdge + revelation.RewardStakedShared;
            baker.OwnStakedBalance += revelation.RewardStakedOwn + revelation.RewardStakedEdge;
            baker.ExternalStakedBalance += revelation.RewardStakedShared;
            baker.NonceRevelationsCount++;

            if (revelation.SenderId != baker.Id)
            {
                Db.TryAttach(sender);
                sender.NonceRevelationsCount++;
            }

            Db.TryAttach(revealedBlock);
            revealedBlock.RevelationId = revelation.Id;

            block.Operations |= Operations.Revelations;

            Cache.AppState.Get().NonceRevelationOpsCount++;
            Cache.Statistics.Current.TotalCreated += revelation.RewardDelegated + revelation.RewardStakedOwn + revelation.RewardStakedEdge + revelation.RewardStakedShared;
            Cache.Statistics.Current.TotalFrozen += revelation.RewardStakedOwn + revelation.RewardStakedEdge + revelation.RewardStakedShared;
            #endregion

            Db.NonceRevelationOps.Add(revelation);
            Context.NonceRevelationOps.Add(revelation);
        }

        public virtual async Task Revert(Block block, NonceRevelationOperation revelation)
        {
            #region entities
            var blockBaker = Context.Proposer;
            var sender = Cache.Accounts.GetDelegate(revelation.SenderId);
            var revealedBlock = await Cache.Blocks.GetAsync(revelation.RevealedLevel);

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(revealedBlock);
            #endregion

            #region apply operation
            blockBaker.Balance -= revelation.RewardDelegated + revelation.RewardStakedOwn + revelation.RewardStakedEdge;
            blockBaker.StakingBalance -= revelation.RewardDelegated + revelation.RewardStakedOwn + revelation.RewardStakedEdge + revelation.RewardStakedShared;
            blockBaker.OwnStakedBalance -= revelation.RewardStakedOwn + revelation.RewardStakedEdge;
            blockBaker.ExternalStakedBalance -= revelation.RewardStakedShared;
            blockBaker.NonceRevelationsCount--;

            if (sender.Id != blockBaker.Id)
            {
                Db.TryAttach(sender);
                sender.NonceRevelationsCount--;
            }

            Db.TryAttach(revealedBlock);
            revealedBlock.RevelationId = null;

            Cache.AppState.Get().NonceRevelationOpsCount--;
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
