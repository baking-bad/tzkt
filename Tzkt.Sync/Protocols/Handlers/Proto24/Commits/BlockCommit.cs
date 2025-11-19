using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto24
{
    class BlockCommit(ProtocolHandler protocol) : Proto19.BlockCommit(protocol)
    {
        public override async Task Apply(JsonElement rawBlock)
        {
            await base.Apply(rawBlock);

            // TODO: rework it when block receipts are updated
            if (Block.Events.HasFlag(BlockEvents.CycleEnd))
            {
                var state = Cache.AppState.Get();
                if (state.AbaActivationLevel is null)
                {
                    var json = await Proto.Node.GetAsync($"chains/main/blocks/{Block.Level}/helpers/validators");
                    if (json.EnumerateArray().First().RequiredInt64("consensus_committee") != 7000)
                        state.AbaActivationLevel = Block.Level;
                }
            }
        }

        public override void Revert (Block block)
        {
            var state = Cache.AppState.Get();
            if (state.AbaActivationLevel == block.Level)
                state.AbaActivationLevel = null;

            base.Revert(block);
        }
    }
}
