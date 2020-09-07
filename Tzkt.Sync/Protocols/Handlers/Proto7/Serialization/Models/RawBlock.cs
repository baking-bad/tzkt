using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Tzkt.Sync.Protocols.Proto7
{
    class RawBlock : IBlock
    {
        [JsonProperty("protocol")]
        [JsonPropertyName("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("chain_id")]
        [JsonPropertyName("chain_id")]
        public string Chain { get; set; }

        [JsonProperty("hash")]
        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonProperty("header")]
        [JsonPropertyName("header")]
        public RawBlockHeader Header { get; set; }

        [JsonProperty("metadata")]
        [JsonPropertyName("metadata")]
        public RawBlockMetadata Metadata { get; set; }

        [JsonProperty("operations")]
        [JsonPropertyName("operations")]
        public List<List<RawOperation>> Operations { get; set; }

        #region IBlock
        public int Level => Header.Level;
        public string Predecessor => Header.Predecessor;
        public int OperationsCount =>
            Operations[0].Count +
            Operations[1].Count +
            Operations[2].Count +
            Operations[3].SelectMany(x => x.Contents).Count() +
            Operations[3].SelectMany(x => x.Contents)
                .Where(x => x is RawTransactionContent tx && tx.Metadata.InternalResults != null)
                .SelectMany(x => (x as RawTransactionContent).Metadata.InternalResults)
                .Count();
        #endregion

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Protocol) &&
            !string.IsNullOrEmpty(Chain) &&
            !string.IsNullOrEmpty(Hash) &&
            Header?.IsValidFormat() == true &&
            Metadata?.IsValidFormat() == true &&
            Operations?.Count == 4 &&
            Operations.All(x => x.All(y => y.IsValidFormat()));
        #endregion
    }

    class RawBlockHeader
    {
        [JsonProperty("level")]
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonProperty("predecessor")]
        [JsonPropertyName("predecessor")]
        public string Predecessor { get; set; }

        [JsonProperty("timestamp")]
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("priority")]
        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        [JsonProperty("proof_of_work_nonce")]
        [JsonPropertyName("proof_of_work_nonce")]
        public string PowNonce { get; set; }

        #region validation
        public bool IsValidFormat() =>
            Level >= 0 &&
            !string.IsNullOrEmpty(Predecessor) &&
            Timestamp != DateTime.MinValue &&
            Priority >= 0 &&
            !string.IsNullOrEmpty(PowNonce);
        #endregion
    }

    class RawBlockMetadata
    {
        [JsonProperty("protocol")]
        [JsonPropertyName("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("next_protocol")]
        [JsonPropertyName("next_protocol")]
        public string NextProtocol { get; set; }

        [JsonProperty("baker")]
        [JsonPropertyName("baker")]
        public string Baker { get; set; }

        [JsonProperty("level")]
        [JsonPropertyName("level")]
        public RawBlockLevel LevelInfo { get; set; }

        [JsonProperty("voting_period_kind")]
        [JsonPropertyName("voting_period_kind")]
        public string VotingPeriod { get; set; }

        [JsonProperty("nonce_hash")]
        [JsonPropertyName("nonce_hash")]
        public string NonceHash { get; set; }

        [JsonProperty("deactivated")]
        [JsonPropertyName("deactivated")]
        public List<string> Deactivated { get; set; }

        [JsonProperty("balance_updates")]
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Protocol) &&
            !string.IsNullOrEmpty(NextProtocol) &&
            !string.IsNullOrEmpty(Baker) &&
            LevelInfo?.IsValidFormat() == true &&
            !string.IsNullOrEmpty(VotingPeriod) &&
            (NonceHash == null || NonceHash != "") &&
            Deactivated != null &&
            BalanceUpdates != null &&
            BalanceUpdates.All(x => x.IsValidFormat());
        #endregion
    }

    class RawBlockLevel
    {
        [JsonProperty("level")]
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonProperty("cycle")]
        [JsonPropertyName("cycle")]
        public int Cycle { get; set; }

        [JsonProperty("voting_period")]
        [JsonPropertyName("voting_period")]
        public int VotingPeriod { get; set; }

        #region validation
        public bool IsValidFormat() =>
            Level >= 0 &&
            Cycle >= 0 &&
            VotingPeriod >= 0;
        #endregion
    }
}
