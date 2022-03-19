using System.Collections.Generic;

namespace Tzkt.Sync.Services
{
    public class DipDupState
    {
        public int LastUpdateId { get; set; } = 0;
        public string Sentinel { get; set; } = "";
    }

    public class TokenMetadataState
    {
        public Dictionary<string, DipDupState> DipDup { get; set; } = new();
        public int LastTokenId { get; set; } = 0;
    }
}
