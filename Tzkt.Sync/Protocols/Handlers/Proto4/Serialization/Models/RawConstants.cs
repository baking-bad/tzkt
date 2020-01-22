using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto4
{
    class RawConstants
    {
        [JsonPropertyName("preserved_cycles")]
        public int PreservedCycles { get; set; }

        [JsonPropertyName("blocks_per_cycle")]
        public int BlocksPerCycle { get; set; }

        [JsonPropertyName("blocks_per_commitment")]
        public int BlocksPerCommitment { get; set; }

        [JsonPropertyName("blocks_per_roll_snapshot")]
        public int BlocksPerSnapshot { get; set; }

        [JsonPropertyName("blocks_per_voting_period")]
        public int BlocksPerVoting { get; set; }

        [JsonPropertyName("time_between_blocks")]
        public List<int> TimeBetweenBlocks { get; set; }

        [JsonPropertyName("endorsers_per_block")]
        public int EndorsersPerBlock { get; set; }

        [JsonPropertyName("hard_gas_limit_per_operation")]
        public int HardOperationGasLimit { get; set; }

        [JsonPropertyName("hard_storage_limit_per_operation")]
        public int HardOperationStorageLimit { get; set; }

        [JsonPropertyName("hard_gas_limit_per_block")]
        public int HardBlockGasLimit { get; set; }

        [JsonPropertyName("tokens_per_roll")]
        public long TokensPerRoll { get; set; }

        [JsonPropertyName("seed_nonce_revelation_tip")]
        public long RevelationReward { get; set; }

        [JsonPropertyName("origination_size")]
        public int OriginationSize { get; set; }

        [JsonPropertyName("block_security_deposit")]
        public long BlockDeposit { get; set; }

        [JsonPropertyName("block_reward")]
        public long BlockReward { get; set; }

        [JsonPropertyName("endorsement_security_deposit")]
        public long EndorsementDeposit { get; set; }

        [JsonPropertyName("endorsement_reward")]
        public long EndorsementReward { get; set; }

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
