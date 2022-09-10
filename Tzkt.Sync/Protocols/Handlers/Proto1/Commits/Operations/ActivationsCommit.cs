using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class ActivationsCommit : ProtocolCommit
    {
        protected ActivationOperation Activation { get; set; }

        public ActivationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var activation = new ActivationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                Account = (User)await Cache.Accounts.GetAsync(content.RequiredString("pkh")),
                Balance = ParseBalance(content.Required("metadata").Required("balance_updates"))
            };

            var btz = Blind.GetBlindedAddress(content.RequiredString("pkh"), content.RequiredString("secret"));
            var commitment = await Db.Commitments.FirstAsync(x => x.Address == btz);
            #endregion

            #region entities
            var sender = activation.Account;
            Db.TryAttach(sender);
            #endregion

            #region apply operation
            sender.Balance += activation.Balance;
            sender.Activated = true;

            block.Operations |= Operations.Activations;

            commitment.AccountId = sender.Id;
            commitment.Level = block.Level;
            #endregion

            Db.ActivationOps.Add(activation);
            Activation = activation;
        }

        public virtual async Task Revert(Block block, ActivationOperation activation)
        {
            #region init
            activation.Block ??= block;
            activation.Account ??= (User)await Cache.Accounts.GetAsync(activation.AccountId);

            var commitment = await Db.Commitments.FirstAsync(x => x.AccountId == activation.AccountId);
            #endregion

            #region entities
            var sender = activation.Account;
            Db.TryAttach(sender);
            #endregion

            #region revert operation
            sender.Balance -= activation.Balance;
            sender.Activated = null;

            commitment.AccountId = null;
            commitment.Level = null;
            #endregion

            Db.ActivationOps.Remove(activation);
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual long ParseBalance(JsonElement balanceUpdates) => balanceUpdates[0].RequiredInt64("change");
    }
}
