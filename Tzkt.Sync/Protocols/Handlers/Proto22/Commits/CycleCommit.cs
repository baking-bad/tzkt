using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto22
{
    class CycleCommit : Proto21.CycleCommit
    {
        public CycleCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override long GetDalAttestationRewardPerShard(JsonElement issuance)
        {
            return issuance.RequiredInt64("dal_attesting_reward_per_shard");
        }
    }
}
