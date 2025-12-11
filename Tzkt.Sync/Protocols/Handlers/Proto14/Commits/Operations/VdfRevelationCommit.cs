using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto14
{
    class VdfRevelationCommit : ProtocolCommit
    {
        public VdfRevelationCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var balanceUpdate = content.Required("metadata").RequiredArray("balance_updates").EnumerateArray()
                   .FirstOrDefault(x => x.RequiredString("kind") == "contract");
            var reward = balanceUpdate.ValueKind != JsonValueKind.Undefined
                ? balanceUpdate.RequiredInt64("change")
                : 0;

            var revelation = new VdfRevelationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                BakerId = Context.Proposer.Id,
                Cycle = block.Cycle,
                RewardDelegated = reward,
                Solution = Hex.Parse(content.RequiredArray("solution", 2)[0].RequiredString()),
                Proof = Hex.Parse(content.RequiredArray("solution", 2)[1].RequiredString())
            };
            #endregion

            #region entities
            var blockBaker = Context.Proposer;
            //Db.TryAttach(blockBaker);
            #endregion

            #region apply operation
            Receive(blockBaker, blockBaker, revelation.RewardDelegated);

            blockBaker.VdfRevelationsCount++;
            Cache.AppState.Get().VdfRevelationOpsCount++;

            block.Operations |= Operations.VdfRevelation;

            Cache.Statistics.Current.TotalCreated += revelation.RewardDelegated;
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
            RevertReceive(blockBaker, blockBaker, revelation.RewardDelegated);

            blockBaker.VdfRevelationsCount--;
            Cache.AppState.Get().VdfRevelationOpsCount--;
            #endregion

            Db.VdfRevelationOps.Remove(revelation);
            Cache.AppState.ReleaseOperationId();
            return Task.CompletedTask;
        }
    }
}
