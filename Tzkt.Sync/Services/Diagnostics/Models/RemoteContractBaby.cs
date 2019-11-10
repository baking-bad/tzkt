using System.Text.Json.Serialization;

namespace Tzkt.Sync.Services.Diagnostics
{
    class RemoteContractBaby
    {
        [JsonPropertyName("balance")]
        public long? Balance { get; set; }

        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }

        [JsonPropertyName("counter")]
        public long? Counter { get; set; }

        #region validation
        public bool IsValidFormat() =>
            Balance != null &&
            Counter != null;
        #endregion
    }
}
