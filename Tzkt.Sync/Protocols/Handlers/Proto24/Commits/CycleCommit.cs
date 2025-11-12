using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto24
{
    class CycleCommit : Proto21.CycleCommit
    {
        public CycleCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override long GetBlockBonusPerSlot(JsonElement issuance)
        {
            return issuance.RequiredInt64("baking_reward_bonus_per_block");
        }

        protected override long GetAttestationBonusPerSlot(JsonElement issuance)
        {
            return issuance.RequiredInt64("attesting_reward_per_block");
        }
    }
}