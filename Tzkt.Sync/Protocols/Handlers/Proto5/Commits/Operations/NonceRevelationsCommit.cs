using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class NonceRevelationsCommit : ProtocolCommit
    {
        public NonceRevelationOperation Revelation { get; private set; }

        NonceRevelationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawNonceRevelationContent content)
        {
            var revealedBlock = await Cache.GetBlockAsync(content.Level);

            Revelation = new NonceRevelationOperation
            {
                Id = await Cache.NextCounterAsync(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.Hash,
                Baker = block.Baker,
                Sender = (Data.Models.Delegate)await Cache.GetAccountAsync(revealedBlock.BakerId),
                RevealedBlock = revealedBlock,
                RevealedLevel = content.Level
            };
        }

        public async Task Init(Block block, NonceRevelationOperation revelation)
        {
            Revelation = revelation;

            Revelation.Block ??= block;
            Revelation.Block.Protocol ??= await Cache.GetProtocolAsync(block.ProtoCode);
            Revelation.Block.Baker ??= (Data.Models.Delegate)await Cache.GetAccountAsync(block.BakerId);

            Revelation.Baker ??= (Data.Models.Delegate)await Cache.GetAccountAsync(revelation.BakerId);
            Revelation.Sender ??= (Data.Models.Delegate)await Cache.GetAccountAsync(revelation.SenderId);
            Revelation.RevealedBlock = await Cache.GetBlockAsync(Revelation.RevealedLevel);
        }

        public override Task Apply()
        {
            #region entities
            var block = Revelation.Block;
            var blockBaker = block.Baker;
            var sender = Revelation.Sender;
            var revealedBlock = Revelation.RevealedBlock;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(revealedBlock);
            #endregion

            #region apply operation
            blockBaker.Balance += block.Protocol.RevelationReward;
            blockBaker.FrozenRewards += block.Protocol.RevelationReward;

            sender.NonceRevelationsCount++;
            if (blockBaker != sender) blockBaker.NonceRevelationsCount++;

            block.Operations |= Operations.Revelations;

            revealedBlock.Revelation = Revelation;
            #endregion

            Db.NonceRevelationOps.Add(Revelation);

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            #region entities
            var block = Revelation.Block;
            var blockBaker = block.Baker;
            var sender = Revelation.Sender;
            var revealedBlock = Revelation.RevealedBlock;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(revealedBlock);
            #endregion

            #region apply operation
            blockBaker.Balance -= block.Protocol.RevelationReward;
            blockBaker.FrozenRewards -= block.Protocol.RevelationReward;

            sender.NonceRevelationsCount--;
            if (blockBaker != sender) blockBaker.NonceRevelationsCount--;

            revealedBlock.Revelation = null;
            revealedBlock.RevelationId = null;
            #endregion

            Db.NonceRevelationOps.Remove(Revelation);

            return Task.CompletedTask;
        }

        #region static
        public static async Task<NonceRevelationsCommit> Apply(ProtocolHandler proto, Block block, RawOperation op, RawNonceRevelationContent content)
        {
            var commit = new NonceRevelationsCommit(proto);
            await commit.Init(block, op, content);
            await commit.Apply();

            return commit;
        }

        public static async Task<NonceRevelationsCommit> Revert(ProtocolHandler proto, Block block, NonceRevelationOperation op)
        {
            var commit = new NonceRevelationsCommit(proto);
            await commit.Init(block, op);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
