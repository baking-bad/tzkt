using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class PreendorsementsCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public void Apply(Block block, JsonElement op, JsonElement content)
        {
            var metadata = content.Required("metadata");
            Apply(block, op.RequiredString("hash"), metadata.RequiredString("delegate"), GetPreendorsedSlots(metadata));
        }

        public void Apply(Block block, string opHash, string bakerAddress, int slots)
        {
            var baker = Cache.Accounts.GetExistingDelegate(bakerAddress);

            var preendorsement = new PreendorsementOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = opHash,
                Slots = slots,
                DelegateId = baker.Id
            };

            Db.TryAttach(baker);
            baker.PreendorsementsCount++;

            block.Operations |= Operations.Preendorsements;

            Cache.AppState.Get().PreendorsementOpsCount++;

            Db.PreendorsementOps.Add(preendorsement);
            Context.PreendorsementOps.Add(preendorsement);
        }

        public Task Revert(Block block, PreendorsementOperation preendorsement)
        {
            var baker = Cache.Accounts.GetDelegate(preendorsement.DelegateId);
            Db.TryAttach(baker);
            baker.PreendorsementsCount--;

            Cache.AppState.Get().PreendorsementOpsCount--;

            Db.PreendorsementOps.Remove(preendorsement);
            Cache.AppState.ReleaseOperationId();

            return Task.CompletedTask;
        }

        protected virtual int GetPreendorsedSlots(JsonElement metadata) => metadata.OptionalInt32("preendorsement_power") ?? metadata.RequiredInt32("consensus_power");
    }
}
