using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class AttestationsCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
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

            var sender = Cache.Accounts.GetExistingDelegate(metadata.RequiredString("delegate"));

            var attestation = new AttestationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                Power = metadata.RequiredArray("slots").Count(),
                DelegateId = sender.Id,
                Reward = reward.ValueKind != JsonValueKind.Undefined ? reward.RequiredInt64("change") : 0,
                Deposit = deposit.ValueKind != JsonValueKind.Undefined ? deposit.RequiredInt64("change") : 0
            };
            #endregion

            #region entities
            Db.TryAttach(sender);
            #endregion

            #region apply operation
            ReceiveLockedRewards(sender, attestation.Reward);

            sender.AttestationsCount++;

            block.Operations |= Operations.Attestations;
            block.AttestationPower += attestation.Power;

            var newDeactivationLevel = sender.Staked ? GracePeriod.Reset(attestation.Level, Context.Protocol) : GracePeriod.Init(attestation.Level, Context.Protocol);
            if (sender.DeactivationLevel < newDeactivationLevel)
            {
                if (sender.DeactivationLevel <= attestation.Level)
                    await ActivateBaker(sender);

                attestation.ResetDeactivation = sender.DeactivationLevel;
                sender.DeactivationLevel = newDeactivationLevel;
            }

            Cache.AppState.Get().AttestationOpsCount++;
            Cache.Statistics.Current.TotalCreated += attestation.Reward;
            Cache.Statistics.Current.TotalFrozen += attestation.Reward + attestation.Deposit;
            #endregion

            //Db.AttestationOps.Add(attestation);
            Context.AttestationOps.Add(attestation);
        }

        public virtual async Task Revert(Block block, AttestationOperation attestation)
        {
            #region entities
            var sender = Cache.Accounts.GetDelegate(attestation.DelegateId);
            Db.TryAttach(sender);
            #endregion

            #region revert operation
            RevertReceiveLockedRewards(sender, attestation.Reward);

            sender.AttestationsCount--;

            if (attestation.ResetDeactivation != null)
            {
                if (attestation.ResetDeactivation <= attestation.Level)
                    await DeactivateBaker(sender);

                sender.DeactivationLevel = (int)attestation.ResetDeactivation;
            }

            Cache.AppState.Get().AttestationOpsCount--;
            #endregion

            //Db.AttestationOps.Remove(attestation);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
