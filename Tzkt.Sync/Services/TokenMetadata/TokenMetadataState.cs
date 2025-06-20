﻿namespace Tzkt.Sync.Services
{
    public class TokenMetadataState
    {
        public Dictionary<string, DipDupState> DipDup { get; set; } = [];
    }

    public class DipDupState
    {
        public long LastUpdateId { get; set; } = 0;
        public long LastTokenId { get; set; } = 0;  // TzKT internal ID
        public int LastIndexedAt { get; set; } = 0; // TzKT internals
        public long LastIndexedAtId { get; set; } = 0; // TzKT internals
        public string Sentinel { get; set; } = string.Empty;
    }
}
