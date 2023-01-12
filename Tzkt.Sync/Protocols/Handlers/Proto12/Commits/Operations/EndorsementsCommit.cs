using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class EndorsementsCommit : ProtocolCommit
    {
        public EndorsementsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            var metadata = content.Required("metadata");
            var baker = Cache.Accounts.GetDelegate(metadata.RequiredString("delegate"));

            var endorsement = new EndorsementOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                Slots = metadata.RequiredInt32("endorsement_power"),
                Delegate = baker,
                DelegateId = baker.Id
            };

            Db.TryAttach(baker);
            baker.EndorsementsCount++;

            #region set baker active
            var newDeactivationLevel = baker.Staked ? GracePeriod.Reset(block) : GracePeriod.Init(block);
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

            Db.EndorsementOps.Add(endorsement);
        }

        public virtual async Task Revert(Block block, EndorsementOperation endorsement)
        {
            endorsement.Block ??= block;
            endorsement.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            endorsement.Delegate ??= Cache.Accounts.GetDelegate(endorsement.DelegateId);

            var baker = endorsement.Delegate;
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

            Db.EndorsementOps.Remove(endorsement);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
