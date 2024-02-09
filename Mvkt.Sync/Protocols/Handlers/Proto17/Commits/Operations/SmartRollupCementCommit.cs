using System.Text.Json;

namespace Mvkt.Sync.Protocols.Proto17
{
    class SmartRollupCementCommit : Proto16.SmartRollupCementCommit
    {
        public SmartRollupCementCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override string GetCommitment(JsonElement content)
            => content.Required("metadata").Required("operation_result").OptionalString("commitment_hash");
    }
}
