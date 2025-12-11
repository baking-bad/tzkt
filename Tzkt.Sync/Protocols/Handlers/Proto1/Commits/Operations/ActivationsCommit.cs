using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class ActivationsCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = (User)await Cache.Accounts.GetOrCreateAsync(content.RequiredString("pkh"));

            var activatedBalance = content
                .Required("metadata")
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Single(x => x.RequiredString("kind") == "contract" && x.RequiredString("contract") == sender.Address)
                .RequiredInt64("change");

            var activation = new ActivationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                AccountId = sender.Id,
                Balance = activatedBalance
            };

            var btz = Blind.GetBlindedAddress(content.RequiredString("pkh"), content.RequiredString("secret"));
            var commitment = await Db.Commitments.FirstAsync(x => x.Address == btz);
            #endregion

            #region apply operation
            Db.TryAttach(sender);
            Receive(sender, activation.Balance);
            sender.ActivationsCount++;

            block.Operations |= Operations.Activations;

            commitment.AccountId = sender.Id;
            commitment.Level = block.Level;

            Cache.AppState.Get().ActivationOpsCount++;
            Cache.Statistics.Current.TotalActivated += activation.Balance;
            #endregion

            Db.ActivationOps.Add(activation);
            Context.ActivationOps.Add(activation);
        }

        public virtual async Task Revert(Block block, ActivationOperation activation)
        {
            #region entities
            var sender = (User)await Cache.Accounts.GetAsync(activation.AccountId);
            var commitment = await Db.Commitments.FirstAsync(x => x.AccountId == activation.AccountId);
            #endregion

            #region revert operation
            Db.TryAttach(sender);
            RevertReceive(sender, activation.Balance);
            sender.ActivationsCount--;

            commitment.AccountId = null;
            commitment.Level = null;

            Cache.AppState.Get().ActivationOpsCount--;
            #endregion

            Db.ActivationOps.Remove(activation);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
