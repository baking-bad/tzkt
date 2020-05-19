using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto4
{
    class StateCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public AppState AppState { get; private set; }
        public string NextProtocol { get; private set; }

        StateCommit(ProtocolHandler protocol) : base(protocol) { }

        public Task Init(Block block, RawBlock rawBlock)
        {
            Block = block;
            NextProtocol = rawBlock.Metadata.NextProtocol;
            AppState = Cache.AppState.Get();
            return Task.CompletedTask;
        }

        public async Task Init(Block block)
        {
            Block = block;
            Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            NextProtocol = block.Protocol.Hash;
            AppState = Cache.AppState.Get();
        }

        public override Task Apply()
        {
            #region entities
            var state = AppState;

            Db.TryAttach(state);
            #endregion

            state.Level = Block.Level;
            state.Timestamp = Block.Timestamp;
            state.Protocol = Block.Protocol.Hash;
            state.NextProtocol = NextProtocol;
            state.Hash = Block.Hash;

            return Task.CompletedTask;
        }

        public override async Task Revert()
        {
            #region entities
            var state = AppState;
            var prevBlock = await Cache.Blocks.PreviousAsync();
            if (prevBlock != null) prevBlock.Protocol ??= await Cache.Protocols.GetAsync(prevBlock.ProtoCode);

            Db.TryAttach(state);
            #endregion

            state.Level = prevBlock?.Level ?? -1;
            state.Timestamp = prevBlock?.Timestamp ?? DateTime.MinValue;
            state.Protocol = prevBlock?.Protocol.Hash ?? "";
            state.NextProtocol = prevBlock == null ? "" : NextProtocol;
            state.Hash = prevBlock?.Hash ?? "";

            Cache.Blocks.Remove(Block);
        }

        #region static
        public static async Task<StateCommit> Apply(ProtocolHandler proto, Block block, RawBlock rawBlock)
        {
            var commit = new StateCommit(proto);
            await commit.Init(block, rawBlock);
            await commit.Apply();

            return commit;
        }

        public static async Task<StateCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new StateCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
