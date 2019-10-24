using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class BlockCommit : ProtocolCommit
    {
        public Block Block { get; protected set; }

        BlockCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(RawBlock rawBlock)
        {
            Block = new Block
            {
                Hash = rawBlock.Hash,
                Level = rawBlock.Level,
                Protocol = await Cache.GetProtocolAsync(rawBlock.Protocol),
                Timestamp = rawBlock.Header.Timestamp,
                Priority = rawBlock.Header.Priority,
                Baker = (Data.Models.Delegate)await Cache.GetAccountAsync(rawBlock.Metadata.Baker)
            };
        }

        public async Task Init(Block block)
        {
            Block = block;
            Block.Protocol ??= await Cache.GetProtocolAsync(block.ProtoCode);
            Block.Baker ??= (Data.Models.Delegate)await Cache.GetAccountAsync(block.BakerId);
        }

        public override Task Apply()
        {
            #region entities
            var baker = Block.Baker;

            Db.TryAttach(baker);
            #endregion

            #region balances
            baker.Balance += Block.Protocol.BlockReward;
            baker.FrozenRewards += Block.Protocol.BlockReward;
            baker.FrozenDeposits += 8_000_000 * ((Block.Level - 1) / Block.Protocol.BlocksPerCycle);
            #endregion

            Db.Blocks.Add(Block);
            Cache.AddBlock(Block);

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            #region entities
            var baker = Block.Baker;

            Db.TryAttach(baker);
            #endregion

            #region balances
            baker.Balance -= Block.Protocol.BlockReward;
            baker.FrozenRewards -= Block.Protocol.BlockReward;
            baker.FrozenDeposits -= 8_000_000 * ((Block.Level - 1) / Block.Protocol.BlocksPerCycle);
            #endregion

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
