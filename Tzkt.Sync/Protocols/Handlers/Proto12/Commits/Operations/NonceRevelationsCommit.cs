using System.Text.Json;
using System.Threading.Tasks;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class NonceRevelationsCommit : ProtocolCommit
    {
        public NonceRevelationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
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
                Reward = block.Protocol.RevelationReward
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
            blockBaker.Balance += revelation.Reward;
            blockBaker.StakingBalance += revelation.Reward;

            sender.NonceRevelationsCount++;
            if (blockBaker != sender) blockBaker.NonceRevelationsCount++;

            block.Operations |= Operations.Revelations;

            revealedBlock.Revelation = revelation;
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
            blockBaker.Balance -= revelation.Reward;
            blockBaker.StakingBalance -= revelation.Reward;

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
