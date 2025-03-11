using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class TransactionsCommit : Proto1.TransactionsCommit
    {
        public TransactionsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override IEnumerable<BigMapDiff>? ParseBigMapDiffs(TransactionOperation transaction, JsonElement result)
        {
            if (!result.TryGetProperty("big_map_diff", out var diffs))
                return null;

            return diffs.RequiredArray().EnumerateArray().Select(x => new UpdateDiff
            {
                Ptr = transaction.TargetId!.Value,
                KeyHash = x.RequiredString("key_hash"),
                Key = x.RequiredMicheline("key"),
                Value = x.OptionalMicheline("value")
            });
        }
    }
}
