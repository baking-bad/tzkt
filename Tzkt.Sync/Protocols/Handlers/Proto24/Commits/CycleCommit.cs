using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto24
{
    class CycleCommit(ProtocolHandler protocol) : Proto22.CycleCommit(protocol)
    {
        protected override long GetBlockBonusPerBlock(JsonElement issuance, Protocol protocol)
            => issuance.RequiredInt64("baking_reward_bonus_per_block");

        protected override long GetAttestationRewardPerBlock(JsonElement issuance, Protocol protocol)
            => issuance.RequiredInt64("attesting_reward_per_block");
    }
}
