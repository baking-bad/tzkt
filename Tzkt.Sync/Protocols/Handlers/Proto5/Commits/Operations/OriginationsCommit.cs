using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class OriginationsCommit(ProtocolHandler protocol) : Proto1.OriginationsCommit(protocol)
    {
        protected override IMicheline GetCode(JsonElement content)
        {
            return Micheline.FromJson(content.Required("script").Required("code"))!;
        }

        protected override IMicheline GetStorage(JsonElement content)
        {
            return Micheline.FromJson(content.Required("script").Required("storage"))!;
        }

        protected override IEnumerable<BigMapDiff>? ParseBigMapDiffs(OriginationOperation origination, JsonElement result, MichelineArray code, IMicheline storage)
        {
            return result.TryGetProperty("big_map_diff", out var diffs)
                ? diffs.RequiredArray().EnumerateArray().Select(BigMapDiff.Parse)
                : null;
        }
    }
}
