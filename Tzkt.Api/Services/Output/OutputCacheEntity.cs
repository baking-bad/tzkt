using System;

namespace Tzkt.Api.Services.Output
{
    public class OutputCacheEntity
    {
        public DateTime LastAccess { get; set; }
        public byte[] Cache { get; set; }
    }
}