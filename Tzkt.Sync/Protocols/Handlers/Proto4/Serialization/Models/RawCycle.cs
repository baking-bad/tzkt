using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto4
{
    class RawCycle
    {
        [JsonPropertyName("random_seed")]
        public string RandomSeed { get; set; }

        [JsonPropertyName("roll_snapshot")]
        public int RollSnapshot { get; set; }

        #region validation
        public bool IsValidFormat() =>
            RandomSeed?.Length == 64 &&
            RollSnapshot >= 0;
        #endregion
    }
}
