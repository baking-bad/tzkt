using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class ActivationsCommit : Proto1.ActivationsCommit
    {
        public ActivationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            await base.Apply(block, op, content);

            var delegat = Cache.Accounts.GetDelegate(Activation.Account.DelegateId)
                ?? Activation.Account as Data.Models.Delegate;

            if (delegat != null)
            {
                Db.TryAttach(delegat);
                delegat.StakingBalance += Activation.Balance;
                if (delegat.Id != Activation.Account.Id)
                    delegat.DelegatedBalance += Activation.Balance;
            }
        }

        public override async Task Revert(Block block, ActivationOperation activation)
        {
            await base.Revert(block, activation);

            var delegat = Cache.Accounts.GetDelegate(activation.Account.DelegateId)
                ?? activation.Account as Data.Models.Delegate;

            if (delegat != null)
            {
                Db.TryAttach(delegat);
                delegat.StakingBalance -= activation.Balance;
                if (delegat.Id != activation.AccountId)
                    delegat.DelegatedBalance -= activation.Balance;
            }
        }
    }
}
