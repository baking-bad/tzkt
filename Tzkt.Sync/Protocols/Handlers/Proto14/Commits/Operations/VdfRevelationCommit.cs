using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                Baker = block.Proposer,
                Cycle = block.Cycle,
                Reward = reward,
                Solution = Hex.Parse(content.RequiredArray("solution", 2)[0].RequiredString()),
                Proof = Hex.Parse(content.RequiredArray("solution", 2)[1].RequiredString())
            };
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            Db.TryAttach(blockBaker);
            #endregion

            #region apply operation
            blockBaker.Balance += revelation.Reward;
            blockBaker.StakingBalance += revelation.Reward;

            blockBaker.VdfRevelationsCount++;
            Cache.AppState.Get().VdfRevelationOpsCount++;

            block.Operations |= Operations.VdfRevelation;
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
            blockBaker.Balance -= revelation.Reward;
            blockBaker.StakingBalance -= revelation.Reward;

            blockBaker.VdfRevelationsCount--;
            Cache.AppState.Get().VdfRevelationOpsCount--;
            #endregion

            Db.VdfRevelationOps.Remove(revelation);
            Cache.AppState.ReleaseOperationId();
            return Task.CompletedTask;
        }
    }
}
