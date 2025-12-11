using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class AttestationsCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public Task Apply(Block block, JsonElement op, JsonElement content)
        {
            var metadata = content.Required("metadata");
            return Apply(block, op.RequiredString("hash"), metadata.RequiredString("delegate"), GetPower(metadata));
        }

        public async Task Apply(Block block, string opHash, string bakerAddress, long power)
        {
            var baker = Cache.Accounts.GetExistingDelegate(bakerAddress);

            var attestation = new AttestationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = opHash,
                Power = power,
                DelegateId = baker.Id
            };

            Db.TryAttach(baker);
            baker.AttestationsCount++;

            #region set baker active
            var newDeactivationLevel = baker.Staked ? GracePeriod.Reset(block.Level, Context.Protocol) : GracePeriod.Init(block.Level, Context.Protocol);
            if (baker.DeactivationLevel < newDeactivationLevel)
            {
                if (baker.DeactivationLevel <= block.Level)
                    await ActivateBaker(baker);

                attestation.ResetDeactivation = baker.DeactivationLevel;
                baker.DeactivationLevel = newDeactivationLevel;
            }
            #endregion

            block.Operations |= Operations.Attestations;
            block.AttestationPower += attestation.Power;

            Cache.AppState.Get().AttestationOpsCount++;

            //Db.AttestationOps.Add(attestation);
            Context.AttestationOps.Add(attestation);
        }

        public async Task Revert(Block block, AttestationOperation attestation)
        {
            var baker = Cache.Accounts.GetDelegate(attestation.DelegateId);
            Db.TryAttach(baker);
            baker.AttestationsCount--;

            #region reset baker activity
            if (attestation.ResetDeactivation != null)
            {
                if (attestation.ResetDeactivation <= block.Level)
                    await DeactivateBaker(baker);

                baker.DeactivationLevel = (int)attestation.ResetDeactivation;
            }
            #endregion

            Cache.AppState.Get().AttestationOpsCount--;

            //Db.AttestationOps.Remove(attestation);
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual long GetPower(JsonElement metadata) => metadata.OptionalInt64("endorsement_power") ?? metadata.RequiredInt64("consensus_power");
    }
}
