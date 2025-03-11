using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto13
{
    class TransactionsCommit(ProtocolHandler protocol) : Proto5.TransactionsCommit(protocol)
    {
        protected override IEnumerable<BigMapDiff>? ParseBigMapDiffs(TransactionOperation transaction, JsonElement result)
        {
            return result.TryGetProperty("lazy_storage_diff", out var diffs)
                ? BigMapDiff.ParseLazyStorage(diffs)
                : null;
        }
    }
}
