using System.Text.Json;
using Netmavryk.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class NonceRevelationsCommit : ProtocolCommit
    {
        public NonceRevelationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var balanceUpdate = content.Required("metadata").RequiredArray("balance_updates").EnumerateArray()
                   .FirstOrDefault(x => x.RequiredString("kind") == "contract");

            var reward = balanceUpdate.ValueKind != JsonValueKind.Undefined
                ? balanceUpdate.RequiredInt64("change")
                : 0;

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
                RewardLiquid = reward
            };
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            var sender = revelation.Sender;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(revealedBlock);
            #endregion

            #region apply operation
            blockBaker.Balance += revelation.RewardLiquid;
            blockBaker.StakingBalance += revelation.RewardLiquid;

            sender.NonceRevelationsCount++;
            if (blockBaker != sender) blockBaker.NonceRevelationsCount++;

            block.Operations |= Operations.Revelations;

            revealedBlock.Revelation = revelation;

            Cache.Statistics.Current.TotalCreated += revelation.RewardLiquid;
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

            #region entities
            var blockBaker = block.Proposer;
            var sender = revelation.Sender;
            var revealedBlock = revelation.RevealedBlock;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(revealedBlock);
            #endregion

            #region apply operation
            blockBaker.Balance -= revelation.RewardLiquid;
            blockBaker.StakingBalance -= revelation.RewardLiquid;

            sender.NonceRevelationsCount--;
            if (blockBaker != sender) blockBaker.NonceRevelationsCount--;

            revealedBlock.Revelation = null;
            revealedBlock.RevelationId = null;
            #endregion

            Db.NonceRevelationOps.Remove(revelation);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
