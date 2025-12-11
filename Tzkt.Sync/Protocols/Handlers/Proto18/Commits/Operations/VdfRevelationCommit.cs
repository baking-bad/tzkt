using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class VdfRevelationCommit : ProtocolCommit
    {
        public VdfRevelationCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var balanceUpdates = content
                .Required("metadata")
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .ToList();

            var (rewardDelegated, rewardStakedOwn, rewardStakedEdge, rewardStakedShared) = ParseRewards(Context.Proposer, balanceUpdates);

            var revelation = new VdfRevelationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                BakerId = Context.Proposer.Id,
                Cycle = block.Cycle,
                Solution = Hex.Parse(content.RequiredArray("solution", 2)[0].RequiredString()),
                Proof = Hex.Parse(content.RequiredArray("solution", 2)[1].RequiredString()),
                RewardDelegated = rewardDelegated,
                RewardStakedOwn = rewardStakedOwn,
                RewardStakedEdge = rewardStakedEdge,
                RewardStakedShared = rewardStakedShared
            };
            #endregion

            #region apply operation
            ReceiveRewards(Context.Proposer, revelation.RewardDelegated, revelation.RewardStakedOwn, revelation.RewardStakedEdge, revelation.RewardStakedShared);
            Context.Proposer.VdfRevelationsCount++;

            Cache.AppState.Get().VdfRevelationOpsCount++;

            block.Operations |= Operations.VdfRevelation;

            Cache.Statistics.Current.TotalCreated += revelation.RewardDelegated + revelation.RewardStakedOwn + revelation.RewardStakedEdge + revelation.RewardStakedShared;
            Cache.Statistics.Current.TotalFrozen += revelation.RewardStakedOwn + revelation.RewardStakedEdge + revelation.RewardStakedShared;
            #endregion

            Db.VdfRevelationOps.Add(revelation);
            Context.VdfRevelationOps.Add(revelation);
            return Task.CompletedTask;
        }

        public virtual Task Revert(Block block, VdfRevelationOperation revelation)
        {
            #region entities
            var blockBaker = Context.Proposer;
            //Db.TryAttach(blockBaker);
            #endregion

            #region apply operation
            RevertReceiveRewards(blockBaker, revelation.RewardDelegated, revelation.RewardStakedOwn, revelation.RewardStakedEdge, revelation.RewardStakedShared);
            blockBaker.VdfRevelationsCount--;

            Cache.AppState.Get().VdfRevelationOpsCount--;
            #endregion

            Db.VdfRevelationOps.Remove(revelation);
            Cache.AppState.ReleaseOperationId();
            return Task.CompletedTask;
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
