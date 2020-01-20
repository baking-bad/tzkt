using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Tzkt.Sync.Protocols.Proto5
{
    class RawConstants
    {
        [JsonProperty("preserved_cycles")]
        [JsonPropertyName("preserved_cycles")]
        public int PreservedCycles { get; set; }

        [JsonProperty("blocks_per_cycle")]
        [JsonPropertyName("blocks_per_cycle")]
        public int BlocksPerCycle { get; set; }

        [JsonProperty("blocks_per_commitment")]
        [JsonPropertyName("blocks_per_commitment")]
        public int BlocksPerCommitment { get; set; }

        [JsonProperty("blocks_per_roll_snapshot")]
        [JsonPropertyName("blocks_per_roll_snapshot")]
        public int BlocksPerSnapshot { get; set; }

        [JsonProperty("blocks_per_voting_period")]
        [JsonPropertyName("blocks_per_voting_period")]
        public int BlocksPerVoting { get; set; }

        [JsonProperty("time_between_blocks")]
        [JsonPropertyName("time_between_blocks")]
        public List<int> TimeBetweenBlocks { get; set; }

        [JsonProperty("endorsers_per_block")]
        [JsonPropertyName("endorsers_per_block")]
        public int EndorsersPerBlock { get; set; }

        [JsonProperty("hard_gas_limit_per_operation")]
        [JsonPropertyName("hard_gas_limit_per_operation")]
        public int HardOperationGasLimit { get; set; }

        [JsonProperty("hard_storage_limit_per_operation")]
        [JsonPropertyName("hard_storage_limit_per_operation")]
        public int HardOperationStorageLimit { get; set; }

        [JsonProperty("hard_gas_limit_per_block")]
        [JsonPropertyName("hard_gas_limit_per_block")]
        public int HardBlockGasLimit { get; set; }

        [JsonProperty("tokens_per_roll")]
        [JsonPropertyName("tokens_per_roll")]
        public long TokensPerRoll { get; set; }

        [JsonProperty("seed_nonce_revelation_tip")]
        [JsonPropertyName("seed_nonce_revelation_tip")]
        public long RevelationReward { get; set; }

        [JsonProperty("origination_size")]
        [JsonPropertyName("origination_size")]
        public int OriginationSize { get; set; }

        [JsonProperty("block_security_deposit")]
        [JsonPropertyName("block_security_deposit")]
        public long BlockDeposit { get; set; }

        [JsonProperty("block_reward")]
        [JsonPropertyName("block_reward")]
        public long BlockReward { get; set; }

        [JsonProperty("endorsement_security_deposit")]
        [JsonPropertyName("endorsement_security_deposit")]
        public long EndorsementDeposit { get; set; }

        [JsonProperty("endorsement_reward")]
        [JsonPropertyName("endorsement_reward")]
        public long EndorsementReward { get; set; }

        [JsonProperty("cost_per_byte")]
        [JsonPropertyName("cost_per_byte")]
        public int ByteCost { get; set; }

        #region validation
        public bool IsValidFormat() =>
            PreservedCycles > 0 &&
            BlocksPerCycle > 0 &&
            BlocksPerCommitment > 0 &&
            BlocksPerSnapshot > 0 &&
            BlocksPerVoting > 0 &&
            TimeBetweenBlocks?.Count > 0 &&
            EndorsersPerBlock > 0 &&
            HardOperationGasLimit > 0 &&
            HardOperationStorageLimit > 0 &&
            HardBlockGasLimit > 0 &&
            TokensPerRoll > 0 &&
            OriginationSize > 0 &&
            ByteCost > 0;
        #endregion
    }
}
