using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class StateCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public AppState AppState { get; private set; }
        public string NextProtocol { get; private set; }

        StateCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawBlock rawBlock)
        {
            Block = block;
            //Block.Protocol ??= await Cache.GetProtocolAsync(rawBlock.Protocol);
            NextProtocol = rawBlock.Metadata.NextProtocol;
            AppState = await Cache.GetAppStateAsync();
        }

        public async Task Init(Block block)
        {
            Block = await Cache.GetCurrentBlockAsync();
            Block.Protocol ??= await Cache.GetProtocolAsync(block.ProtoCode);
            NextProtocol = block.Protocol.Hash;
            AppState = await Cache.GetAppStateAsync();
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
            var prevBlock = await Cache.GetPreviousBlockAsync();
            if (prevBlock != null) prevBlock.Protocol ??= await Cache.GetProtocolAsync(prevBlock.ProtoCode);

            Db.TryAttach(state);
            #endregion

            state.Level = prevBlock?.Level ?? -1;
            state.Timestamp = prevBlock?.Timestamp ?? DateTime.MinValue;
            state.Protocol = prevBlock?.Protocol.Hash ?? "";
            state.NextProtocol = prevBlock == null ? "" : NextProtocol;
            state.Hash = prevBlock?.Hash ?? "";

            Cache.RemoveBlock(Block);
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
