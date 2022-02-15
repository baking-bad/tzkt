using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
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
                Baker = block.Baker,
                Sender = Cache.Accounts.GetDelegate(revealedBlock.BakerId),
                RevealedBlock = revealedBlock,
                RevealedLevel = revealedBlock.Level
            };
            #endregion

            #region entities
            //var block = revelation.Block;
            var blockBaker = block.Baker;
            var sender = revelation.Sender;
            //var revealedBlock = revelation.RevealedBlock;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(revealedBlock);
            #endregion

            #region apply operation
            blockBaker.Balance += block.Protocol.RevelationReward;

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
            revelation.Block.Baker ??= Cache.Accounts.GetDelegate(block.BakerId);

            revelation.Baker ??= Cache.Accounts.GetDelegate(revelation.BakerId);
            revelation.Sender ??= Cache.Accounts.GetDelegate(revelation.SenderId);
            revelation.RevealedBlock = await Cache.Blocks.GetAsync(revelation.RevealedLevel);
            #endregion

            #region entities
            //var block = revelation.Block;
            var blockBaker = block.Baker;
            var sender = revelation.Sender;
            var revealedBlock = revelation.RevealedBlock;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(revealedBlock);
            #endregion

            #region apply operation
            blockBaker.Balance -= block.Protocol.RevelationReward;

            sender.NonceRevelationsCount--;
            if (blockBaker != sender) blockBaker.NonceRevelationsCount--;

            revealedBlock.Revelation = null;
            revealedBlock.RevelationId = null;
            #endregion

            Db.NonceRevelationOps.Remove(revelation);
        }
    }
}
