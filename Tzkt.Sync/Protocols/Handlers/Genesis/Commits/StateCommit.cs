using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Genesis
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
            AppState = await Cache.GetAppStateAsync();
            NextProtocol = rawBlock.Metadata.NextProtocol;
        }

        public async Task Init(Block block)
        {
            Block = await Cache.GetCurrentBlockAsync();
            Block.Protocol ??= await Cache.GetProtocolAsync(block.ProtoCode);
            AppState = await Cache.GetAppStateAsync();
            NextProtocol = block.Protocol.Hash;
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

        public override Task Revert()
        {
            #region entities
            var state = AppState;

            Db.TryAttach(state);
            #endregion

            state.Level = -1;
            state.Timestamp = DateTime.MinValue;
            state.Protocol = "";
            state.NextProtocol = "";
            state.Hash = "";

            Cache.RemoveBlock(Block);
            return Task.CompletedTask;
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
