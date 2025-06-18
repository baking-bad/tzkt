using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class PreattestationsCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public void Apply(Block block, JsonElement op, JsonElement content)
        {
            var metadata = content.Required("metadata");
            Apply(block, op.RequiredString("hash"), metadata.RequiredString("delegate"), GetPreattestedSlots(metadata));
        }

        public void Apply(Block block, string opHash, string bakerAddress, int slots)
        {
            var baker = Cache.Accounts.GetExistingDelegate(bakerAddress);

            var preattestation = new PreattestationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = opHash,
                Slots = slots,
                DelegateId = baker.Id
            };

            Db.TryAttach(baker);
            baker.PreattestationsCount++;

            block.Operations |= Operations.Preattestations;

            Cache.AppState.Get().PreattestationOpsCount++;

            Db.PreattestationOps.Add(preattestation);
            Context.PreattestationOps.Add(preattestation);
        }

        public Task Revert(Block block, PreattestationOperation preattestation)
        {
            var baker = Cache.Accounts.GetDelegate(preattestation.DelegateId);
            Db.TryAttach(baker);
            baker.PreattestationsCount--;

            Cache.AppState.Get().PreattestationOpsCount--;

            Db.PreattestationOps.Remove(preattestation);
            Cache.AppState.ReleaseOperationId();

            return Task.CompletedTask;
        }

        protected virtual int GetPreattestedSlots(JsonElement metadata) => metadata.OptionalInt32("preendorsement_power") ?? metadata.RequiredInt32("consensus_power");
    }
}
