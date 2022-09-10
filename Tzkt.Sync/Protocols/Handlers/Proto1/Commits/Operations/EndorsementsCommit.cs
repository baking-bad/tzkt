using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class EndorsementsCommit : ProtocolCommit
    {
        public EndorsementsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var metadata = content.Required("metadata");
            var reward = metadata
                    .RequiredArray("balance_updates")
                    .EnumerateArray()
                    .FirstOrDefault(x => x.RequiredString("kind")[0] == 'f' && x.RequiredString("category")[0] == 'r');
            var deposit = metadata
                    .RequiredArray("balance_updates")
                    .EnumerateArray()
                    .FirstOrDefault(x => x.RequiredString("kind")[0] == 'f' && x.RequiredString("category")[0] == 'd');

            var endorsement = new EndorsementOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                Slots = metadata.RequiredArray("slots").Count(),
                Delegate = Cache.Accounts.GetDelegate(metadata.RequiredString("delegate")),
                Reward = reward.ValueKind != JsonValueKind.Undefined ? reward.RequiredInt64("change") : 0,
                Deposit = deposit.ValueKind != JsonValueKind.Undefined ? deposit.RequiredInt64("change") : 0
            };
            #endregion

            #region entities
            //var block = endorsement.Block;
            var sender = endorsement.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(sender);
            #endregion

            #region apply operation
            sender.Balance += endorsement.Reward;

            sender.EndorsementsCount++;

            block.Operations |= Operations.Endorsements;
            block.Validations += endorsement.Slots;

            var newDeactivationLevel = sender.Staked ? GracePeriod.Reset(endorsement.Block) : GracePeriod.Init(endorsement.Block);
            if (sender.DeactivationLevel < newDeactivationLevel)
            {
                if (sender.DeactivationLevel <= endorsement.Level)
                    await UpdateDelegate(sender, true);

                endorsement.ResetDeactivation = sender.DeactivationLevel;
                sender.DeactivationLevel = newDeactivationLevel;
            }
            #endregion

            Db.EndorsementOps.Add(endorsement);
        }

        public virtual async Task Revert(Block block, EndorsementOperation endorsement)
        {
            #region init
            endorsement.Block ??= block;
            endorsement.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            endorsement.Delegate ??= Cache.Accounts.GetDelegate(endorsement.DelegateId);
            #endregion

            #region entities
            //var block = endorsement.Block;
            var sender = endorsement.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(sender);
            #endregion

            #region revert operation
            sender.Balance -= endorsement.Reward;

            sender.EndorsementsCount--;

            if (endorsement.ResetDeactivation != null)
            {
                if (endorsement.ResetDeactivation <= endorsement.Level)
                    await UpdateDelegate(sender, false);

                sender.DeactivationLevel = (int)endorsement.ResetDeactivation;
            }
            #endregion

            Db.EndorsementOps.Remove(endorsement);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
