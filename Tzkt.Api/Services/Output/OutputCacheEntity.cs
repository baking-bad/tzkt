using System;

namespace Tzkt.Api.Services.Output
{
    public class OutputCacheEntity
    {
        public DateTime LastAccess { get; set; }
        public byte[] Bytes { get; init; }
        public bool IsCompressed { get; init; }
    }
}