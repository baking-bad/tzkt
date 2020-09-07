using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto7
{
    class RawEndorsingRight
    {
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("slots")]
        public List<int> Slots { get; set; }

        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }

        #region validation
        public bool IsValidFormat() =>
            Level >= 0 &&
            Slots?.Count > 0 &&
            !string.IsNullOrEmpty(Delegate);
        #endregion
    }
}
