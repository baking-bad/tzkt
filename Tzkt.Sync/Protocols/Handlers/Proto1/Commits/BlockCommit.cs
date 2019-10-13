using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class BlockCommit : ProtocolCommit
    {
        public Block Block { get; protected set; }
        public Protocol Protocol { get; private set; }

        public BlockCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init()
        {
            Protocol = await Cache.GetCurrentProtocolAsync();
            Block = await Cache.GetCurrentBlockAsync();
        }

        public override async Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;

            Protocol = await Cache.GetProtocolAsync(block.Protocol);
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

        public override Task Apply()
        {
            if (Block == null)
                throw new Exception("Commit is not initialized");

            #region entities
            var baker = Block.Baker;

            Db.TryAttach(baker);
            #endregion

            #region balances
            baker.Balance += Protocol.BlockReward;
            baker.FrozenRewards += Protocol.BlockReward;
            baker.FrozenDeposits += Protocol.BlockDeposit;
            #endregion

            Db.Blocks.Add(Block);
            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            if (Block == null)
                throw new Exception("Commit is not initialized");

            #region entities
            var baker = Block.Baker;

            Db.TryAttach(baker);
            #endregion

            #region balances
            baker.Balance -= Protocol.BlockReward;
            baker.FrozenRewards -= Protocol.BlockReward;
            baker.FrozenDeposits -= Protocol.BlockDeposit;
            #endregion

            Db.Blocks.Remove(Block);            
            return Task.CompletedTask;
        }

        #region static
        public static async Task<BlockCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new BlockCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static async Task<BlockCommit> Create(ProtocolHandler protocol, List<ICommit> commits)
        {
            var commit = new BlockCommit(protocol, commits);
            await commit.Init();
            return commit;
        }
        #endregion
    }
}
