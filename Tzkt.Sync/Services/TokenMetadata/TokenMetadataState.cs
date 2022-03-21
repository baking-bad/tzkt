using System.Collections.Generic;

namespace Tzkt.Sync.Services
{
    public class TokenMetadataState
    {
        public Dictionary<string, DipDupState> DipDup { get; set; } = new();
    }

    public class DipDupState
    {
        public int LastUpdateId { get; set; } = 0;
        public int LastTokenId { get; set; } = 0;  // TzKT internal ID
        public string Sentinel { get; set; } = string.Empty;
    }
}
