using System.Collections.Generic;
using System.Text.Json;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto13
{
    class TransactionsCommit : Proto5.TransactionsCommit
    {
        public TransactionsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override IEnumerable<BigMapDiff> ParseBigMapDiffs(TransactionOperation transaction, JsonElement result)
        {
            return result.TryGetProperty("lazy_storage_diff", out var diffs)
                ? BigMapDiff.ParseLazyStorage(diffs)
                : null;
        }
    }
}
