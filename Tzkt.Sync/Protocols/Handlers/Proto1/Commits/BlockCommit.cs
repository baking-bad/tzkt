using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class BlockCommit : ProtocolCommit
    {
        #region constants
        protected virtual int PreservedCycles => 5;

        protected virtual int BlocksPerCycle => 4096;
        protected virtual int BlocksPerCommitment => 32;
        protected virtual int BlocksPerSnapshot => 256;
        protected virtual int BlocksPerVoting => 32_768;

        protected virtual int TokensPerRoll => 10_000;

        protected virtual int ByteCost => 1000;
        protected virtual int OriginationCost => 257_000;
        protected virtual int NonceRevelationReward => 125_000;

        protected virtual int BlockDeposit => 0;
        protected virtual int EndorsementDeposit => 0;

        protected virtual int BlockReward => 0;
        protected virtual int EndorsementReward => 0;
        #endregion

        public Block Block { get; protected set; }

        public BlockCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init()
        {
            Block = await Cache.GetCurrentBlockAsync();
        }

        public override async Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;

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
            baker.Balance += BlockReward;
            baker.FrozenRewards += BlockReward;
            baker.FrozenDeposits += BlockDeposit;
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
            baker.Balance -= BlockReward;
            baker.FrozenRewards -= BlockReward;
            baker.FrozenDeposits -= BlockDeposit;
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
