using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class EndorsementsCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public Task Apply(Block block, JsonElement op, JsonElement content)
        {
            var metadata = content.Required("metadata");
            return Apply(block, op.RequiredString("hash"), metadata.RequiredString("delegate"), GetEndorsedSlots(metadata));
        }

        public async Task Apply(Block block, string opHash, string bakerAddress, int slots)
        {
            var baker = Cache.Accounts.GetExistingDelegate(bakerAddress);

            var endorsement = new EndorsementOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = opHash,
                Slots = slots,
                DelegateId = baker.Id
            };

            Db.TryAttach(baker);
            baker.EndorsementsCount++;

            #region set baker active
            var newDeactivationLevel = baker.Staked ? GracePeriod.Reset(block.Level, Context.Protocol) : GracePeriod.Init(block.Level, Context.Protocol);
            if (baker.DeactivationLevel < newDeactivationLevel)
            {
                if (baker.DeactivationLevel <= block.Level)
                    await UpdateDelegate(baker, true);

                endorsement.ResetDeactivation = baker.DeactivationLevel;
                baker.DeactivationLevel = newDeactivationLevel;
            }
            #endregion

            block.Operations |= Operations.Endorsements;
            block.Validations += endorsement.Slots;

            Cache.AppState.Get().EndorsementOpsCount++;

            //Db.EndorsementOps.Add(endorsement);
            Context.EndorsementOps.Add(endorsement);
        }

        public async Task Revert(Block block, EndorsementOperation endorsement)
        {
            var baker = Cache.Accounts.GetDelegate(endorsement.DelegateId);
            Db.TryAttach(baker);
            baker.EndorsementsCount--;

            #region reset baker activity
            if (endorsement.ResetDeactivation != null)
            {
                if (endorsement.ResetDeactivation <= block.Level)
                    await UpdateDelegate(baker, false);

                baker.DeactivationLevel = (int)endorsement.ResetDeactivation;
            }
            #endregion

            Cache.AppState.Get().EndorsementOpsCount--;

            //Db.EndorsementOps.Remove(endorsement);
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual int GetEndorsedSlots(JsonElement metadata) => metadata.OptionalInt32("endorsement_power") ?? metadata.RequiredInt32("consensus_power");
    }
}
