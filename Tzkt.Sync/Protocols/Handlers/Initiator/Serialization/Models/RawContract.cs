using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Initiator
{
    class RawContract
    {
        [JsonIgnore]
        public string Address { get; set; }

        [JsonPropertyName("balance")]
        public long Balance { get; set; }

        [JsonPropertyName("counter")]
        public int Counter { get; set; }

        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }

        [JsonPropertyName("manager")]
        public string Manager { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Address) &&
            Balance >= 0 &&
            Counter >= 0 &&
            (Delegate == null || Delegate != "") &&
            !string.IsNullOrEmpty(Manager);
        #endregion
    }
}
