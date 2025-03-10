using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class StateCommit : ProtocolCommit
    {
        public StateCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual Task Apply(Block block, JsonElement rawBlock)
        {
            var nextProtocol = rawBlock.Required("metadata").RequiredString("next_protocol");
            var appState = Cache.AppState.Get();

            #region entities
            var state = appState;
            #endregion

            state.Cycle = block.Cycle;
            state.Level = block.Level;
            state.Timestamp = block.Timestamp;
            state.Protocol = Context.Protocol.Hash;
            state.NextProtocol = nextProtocol;
            state.Hash = block.Hash;

            if (block.Events.HasFlag(BlockEvents.ProtocolBegin)) state.ProtocolsCount++;
            if (block.Events.HasFlag(BlockEvents.CycleBegin)) state.CyclesCount++;

            return Task.CompletedTask;
        }

        public virtual async Task Revert(Block block)
        {
            var nextProtocol = Context.Protocol.Hash;
            var appState = Cache.AppState.Get();

            #region entities
            var state = appState;
            var prevBlock = await Cache.Blocks.PreviousAsync();
            var prevProtocol = await Cache.Protocols.GetAsync(prevBlock.ProtoCode);
            #endregion

            state.Cycle = prevBlock.Cycle;
            state.Level = prevBlock.Level;
            state.Timestamp = prevBlock.Timestamp;
            state.Protocol = prevProtocol.Hash;
            state.NextProtocol = nextProtocol;
            state.Hash = prevBlock.Hash;

            if (block.Events.HasFlag(BlockEvents.ProtocolBegin)) state.ProtocolsCount--;
            if (block.Events.HasFlag(BlockEvents.CycleBegin)) state.CyclesCount--;

            Cache.Blocks.Remove(block);
        }
    }
}
