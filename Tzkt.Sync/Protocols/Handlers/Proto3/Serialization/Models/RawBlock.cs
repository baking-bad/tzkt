using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto3
{
    class RawBlock : IBlock
    {
        [JsonPropertyName("protocol")]
        public string Protocol { get; set; }

        [JsonPropertyName("chain_id")]
        public string Chain { get; set; }

        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonPropertyName("header")]
        public RawBlockHeader Header { get; set; }

        [JsonPropertyName("metadata")]
        public RawBlockMetadata Metadata { get; set; }

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
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("predecessor")]
        public string Predecessor { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        #region validation
        public bool IsValidFormat() =>
            Level >= 0 &&
            !string.IsNullOrEmpty(Predecessor) &&
            Timestamp != DateTime.MinValue &&
            Priority >= 0;
        #endregion
    }

    class RawBlockMetadata
    {
        [JsonPropertyName("protocol")]
        public string Protocol { get; set; }

        [JsonPropertyName("next_protocol")]
        public string NextProtocol { get; set; }

        [JsonPropertyName("baker")]
        public string Baker { get; set; }

        [JsonPropertyName("level")]
        public RawBlockLevel LevelInfo { get; set; }

        [JsonPropertyName("voting_period_kind")]
        public string VotingPeriod { get; set; }

        [JsonPropertyName("nonce_hash")]
        public string NonceHash { get; set; }

        [JsonPropertyName("deactivated")]
        public List<string> Deactivated { get; set; }

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
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("cycle")]
        public int Cycle { get; set; }

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
