using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Initiator
{
    class RawProtoParameters
    {
        [JsonPropertyName("commitments")]
        public List<List<string>> Commitments { get; set; }

        [JsonPropertyName("security_deposit_ramp_up_cycles")]
        public int SecurityDepositRampUp { get; set; }

        [JsonPropertyName("no_reward_cycles")]
        public int NoRewardCycles { get; set; }
    }
}
