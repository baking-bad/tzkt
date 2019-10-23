using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Genesis
{
    class BlockCommit : ProtocolCommit
    {
        public Block Block { get; private set; }

        BlockCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(RawBlock rawBlock)
        {
            Block = new Block
            {
                Hash = rawBlock.Hash,
                Level = rawBlock.Level,
                Protocol = await Cache.GetProtocolAsync(rawBlock.Protocol),
                Timestamp = rawBlock.Header.Timestamp
            };
        }

        public Task Init(Block block)
        {
            Block = block;
            return Task.CompletedTask;
        }

        public override Task Apply()
        {
            Db.Blocks.Add(Block);
            Cache.AddBlock(Block);

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            Db.Blocks.Remove(Block);
            return Task.CompletedTask;
        }

        #region static
        public static async Task<BlockCommit> Apply(ProtocolHandler proto, RawBlock rawBlock)
        {
            var commit = new BlockCommit(proto);
            await commit.Init(rawBlock);
            await commit.Apply();

            return commit;
        }

        public static async Task<BlockCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new BlockCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
