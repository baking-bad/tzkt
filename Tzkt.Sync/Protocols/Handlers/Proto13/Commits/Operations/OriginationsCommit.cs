using System.Collections.Generic;
using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto13
{
    class OriginationsCommit : Proto5.OriginationsCommit
    {
        public OriginationsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override IEnumerable<BigMapDiff> ParseBigMapDiffs(OriginationOperation origination, JsonElement result, MichelineArray code, IMicheline storage)
        {
            return result.TryGetProperty("lazy_storage_diff", out var diffs)
                ? BigMapDiff.ParseLazyStorage(diffs)
                : null;
        }
    }
}
