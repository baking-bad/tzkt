using System.Text.Json;
using Mvkt.Data.Models;
using Mvkt.Data.Models.Base;

namespace Mvkt.Sync.Protocols.Proto14
{
    class RevealsCommit : Proto1.RevealsCommit
    {
        public RevealsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetConsumedGas(JsonElement result)
        {
            return (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000);
        }

        protected override void ApplyResult(RevealOperation op, string pubKey)
        {
            if (op.Status != OperationStatus.Applied) return;
            base.ApplyResult(op, pubKey);
        }

        protected override void RevertResult(RevealOperation op)
        {
            if (op.Status != OperationStatus.Applied) return;
            base.RevertResult(op);
        }
    }
}
