using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto13
{
    class OriginationsCommit(ProtocolHandler protocol) : Proto5.OriginationsCommit(protocol)
    {
        protected override IEnumerable<BigMapDiff>? ParseBigMapDiffs(OriginationOperation origination, JsonElement result, MichelineArray code, IMicheline storage)
        {
            return result.TryGetProperty("lazy_storage_diff", out var diffs)
                ? BigMapDiff.ParseLazyStorage(diffs)
                : null;
        }
    }
}
