using System.Text.Json;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto14
{
    class RevealsCommit : Proto1.RevealsCommit
    {
        public RevealsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetConsumedGas(JsonElement result)
        {
            return (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000);
        }

        protected override void ApplyResult(RevealOperation op, Account sender, string pubKey)
        {
            if (op.Status != OperationStatus.Applied) return;
            base.ApplyResult(op, sender, pubKey);
        }

        protected override void RevertResult(RevealOperation op, Account sender)
        {
            if (op.Status != OperationStatus.Applied) return;
            base.RevertResult(op, sender);
        }
    }
}
