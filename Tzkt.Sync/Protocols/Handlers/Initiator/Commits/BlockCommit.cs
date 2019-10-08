using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Initiator
{
    class BlockCommit : ProtocolCommit
    {
        public Block Block { get; private set; }

        public BlockCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;

            Block = new Block
            {
                Hash = rawBlock.Hash,
                Level = rawBlock.Level,
                Protocol = await Protocols.GetProtocolAsync(rawBlock.Protocol),
                Timestamp = rawBlock.Header.Timestamp
            };
        }

        public override Task Apply()
        {
            if (Block == null)
                throw new Exception("Commit is not initialized");

            Db.Blocks.Add(Block);
            Protocols.ProtocolUp(Block.Protocol);

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            if (Block == null)
                throw new Exception("Commit is not initialized");

            Db.Blocks.Remove(Block);
            Protocols.ProtocolDown(Block.Protocol);

            return Task.CompletedTask;
        }

        #region static
        public static async Task<BlockCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new BlockCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static Task<BlockCommit> Create(ProtocolHandler protocol, List<ICommit> commits, Block block)
        {
            var commit = new BlockCommit(protocol, commits) { Block = block };
            return Task.FromResult(commit);
        }
        #endregion
    }
}
