using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto24
{
    class BlockCommit(ProtocolHandler protocol) : Proto19.BlockCommit(protocol)
    {
        public override async Task Apply(JsonElement rawBlock)
        {
            await base.Apply(rawBlock);

            var state = Cache.AppState.Get();
            if (state.AbaActivationLevel is null)
            {
                var abaLevel = rawBlock.Required("metadata").Optional("all_bakers_attest_activation_level")?.RequiredInt32("level");
                if (abaLevel == Block.Level)
                    state.AbaActivationLevel = abaLevel;
            }
        }

        public override void Revert (Block block)
        {
            var state = Cache.AppState.Get();
            if (state.AbaActivationLevel == block.Level)
                state.AbaActivationLevel = null;

            base.Revert(block);
        }

        protected override long GetAttestationCommittee(Protocol protocol, JsonElement metadata)
            => metadata.Optional("attestations")?.RequiredInt64("total_committee_power") ?? 0L;
    }
}
