using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto24
{
    class StateCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual Task Apply(Block block, JsonElement rawBlock)
        {
            var state = Cache.AppState.Get();
            
            state.Hash = block.Hash;
            state.Level = block.Level;
            state.Timestamp = block.Timestamp;
            state.Cycle = block.Cycle;
            state.Protocol = Context.Protocol.Hash;
            state.NextProtocol = rawBlock.Required("metadata").RequiredString("next_protocol");

            return Task.CompletedTask;
        }

        public virtual async Task Revert()
        {
            var state = Cache.AppState.Get();

            if (state.BlocksCount == 0)
            {
                state.Hash = string.Empty;
                state.Level = state.Level - 1;
                state.Timestamp = DateTimeOffset.MinValue.UtcDateTime;
                state.Cycle = -1;
                state.Protocol = string.Empty;
                state.NextProtocol = Context.Protocol.Hash;
            }
            else
            {
                var prevBlock = await Cache.Blocks.PreviousAsync();
                var prevProtocol = await Cache.Protocols.GetAsync(prevBlock.ProtoCode);

                state.Hash = prevBlock.Hash;
                state.Level = prevBlock.Level;
                state.Timestamp = prevBlock.Timestamp;
                state.Cycle = prevBlock.Cycle;
                state.Protocol = prevProtocol.Hash;
                state.NextProtocol = Context.Protocol.Hash;
            }
        }
    }
}
