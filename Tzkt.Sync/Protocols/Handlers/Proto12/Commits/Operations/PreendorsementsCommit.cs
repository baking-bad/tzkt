using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class PreendorsementsCommit : ProtocolCommit
    {
        public PreendorsementsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual void Apply(Block block, JsonElement op, JsonElement content)
        {
            var metadata = content.Required("metadata");
            var baker = Cache.Accounts.GetDelegate(metadata.RequiredString("delegate"));

            var preendorsement = new PreendorsementOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                Slots = metadata.RequiredInt32("preendorsement_power"),
                Delegate = baker,
                DelegateId = baker.Id
            };

            Db.TryAttach(baker);
            baker.PreendorsementsCount++;

            block.Operations |= Operations.Preendorsements;

            Db.PreendorsementOps.Add(preendorsement);
        }

        public virtual async Task Revert(Block block, PreendorsementOperation preendorsement)
        {
            preendorsement.Block ??= block;
            preendorsement.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            preendorsement.Delegate ??= Cache.Accounts.GetDelegate(preendorsement.DelegateId);

            var baker = preendorsement.Delegate;
            Db.TryAttach(baker);
            baker.PreendorsementsCount--;

            Db.PreendorsementOps.Remove(preendorsement);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
