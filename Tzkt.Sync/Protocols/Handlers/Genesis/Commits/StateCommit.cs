using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Genesis
{
    class StateCommit : ProtocolCommit
    {
        public AppState AppState { get; private set; }
        public Block Block { get; private set; }
        public string NextProtocol { get; private set; }

        public StateCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init()
        {
            Block = await Cache.GetCurrentBlockAsync();
            Block.Protocol ??= await Cache.GetCurrentProtocolAsync();
            NextProtocol = Block.Protocol.Hash;
            AppState = await Cache.GetAppStateAsync();
        }

        public override async Task Init(IBlock block)
        {
            Block = FindCommit<BlockCommit>().Block;
            Block.Protocol ??= await Cache.GetProtocolAsync(block.Protocol);
            NextProtocol = (block as RawBlock).Metadata.NextProtocol;
            AppState = await Cache.GetAppStateAsync();
        }

        public override async Task Apply()
        {
            if (AppState == null)
                throw new Exception("Commit is not initialized");

            #region entities
            var state = AppState;

            Db.TryAttach(state);
            #endregion

            state.Level = Block.Level;
            state.Timestamp = Block.Timestamp;
            state.Protocol = Block.Protocol.Hash;
            state.NextProtocol = NextProtocol;
            state.Hash = Block.Hash;
            await Cache.PushBlock(Block);
        }

        public override async Task Revert()
        {
            if (AppState == null)
                throw new Exception("Commit is not initialized");

            #region entities
            var prevBlock = await Cache.GetPreviousBlockAsync();
            if (prevBlock != null) prevBlock.Protocol ??= await Cache.GetProtocolAsync(prevBlock.ProtoCode);

            var state = AppState;

            Db.TryAttach(state);
            #endregion

            state.Level = prevBlock?.Level ?? -1;
            state.Timestamp = prevBlock?.Timestamp ?? DateTime.MinValue;
            state.Protocol = prevBlock?.Protocol.Hash ?? "";
            state.NextProtocol = prevBlock == null ? "" : Block.Protocol.Hash;
            state.Hash = prevBlock?.Hash ?? "";
            await Cache.PopBlock();
        }

        #region static
        public static async Task<StateCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new StateCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static async Task<StateCommit> Create(ProtocolHandler protocol, List<ICommit> commits)
        {
            var commit = new StateCommit(protocol, commits);
            await commit.Init();
            return commit;
        }
        #endregion
    }
}
