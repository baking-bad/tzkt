using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class NonceRevelationsCommit : ProtocolCommit
    {
        public NonceRevelationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var balanceUpdate = content.Required("metadata").RequiredArray("balance_updates").EnumerateArray()
                   .FirstOrDefault(x => x.RequiredString("kind") == "freezer" && x.RequiredString("category") == "rewards");

            var reward = balanceUpdate.ValueKind != JsonValueKind.Undefined
                ? balanceUpdate.RequiredInt64("change")
                : 0;

            var revealedBlock = await Cache.Blocks.GetAsync(content.RequiredInt32("level"));
            var sender = Cache.Accounts.GetDelegate(revealedBlock.ProposerId);

            var revelation = new NonceRevelationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                BakerId = Context.Proposer.Id,
                SenderId = sender.Id,
                RevealedLevel = revealedBlock.Level,
                RevealedCycle = revealedBlock.Cycle,
                Nonce = Hex.Parse(content.RequiredString("nonce")),
                RewardDelegated = reward
            };
            #endregion

            #region entities
            var blockBaker = Context.Proposer;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(revealedBlock);
            #endregion

            #region apply operation
            blockBaker.Balance += revelation.RewardDelegated;

            sender.NonceRevelationsCount++;
            if (blockBaker != sender) blockBaker.NonceRevelationsCount++;

            block.Operations |= Operations.Revelations;

            revealedBlock.RevelationId = revelation.Id;

            Cache.AppState.Get().NonceRevelationOpsCount++;
            Cache.Statistics.Current.TotalCreated += revelation.RewardDelegated;
            Cache.Statistics.Current.TotalFrozen += revelation.RewardDelegated;
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
            blockBaker.Balance -= revelation.RewardDelegated;

            sender.NonceRevelationsCount--;
            if (blockBaker != sender) blockBaker.NonceRevelationsCount--;

            revealedBlock.RevelationId = null;

            Cache.AppState.Get().NonceRevelationOpsCount--;
            #endregion

            Db.NonceRevelationOps.Remove(revelation);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
