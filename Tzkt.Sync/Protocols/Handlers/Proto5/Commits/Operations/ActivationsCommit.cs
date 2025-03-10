using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class ActivationsCommit : Proto1.ActivationsCommit
    {
        public ActivationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            await base.Apply(block, op, content);

            var account = await Cache.Accounts.GetAsync(Activation.AccountId);
            var delegat = Cache.Accounts.GetDelegate(account.DelegateId) ?? account as Data.Models.Delegate;

            if (delegat != null)
            {
                Db.TryAttach(delegat);
                delegat.StakingBalance += Activation.Balance;
                if (delegat.Id != account.Id)
                    delegat.DelegatedBalance += Activation.Balance;
            }
        }

        public override async Task Revert(Block block, ActivationOperation activation)
        {
            await base.Revert(block, activation);

            var account = await Cache.Accounts.GetAsync(activation.AccountId);
            var delegat = Cache.Accounts.GetDelegate(account.DelegateId) ?? account as Data.Models.Delegate;

            if (delegat != null)
            {
                Db.TryAttach(delegat);
                delegat.StakingBalance -= activation.Balance;
                if (delegat.Id != account.Id)
                    delegat.DelegatedBalance -= activation.Balance;
            }
        }
    }
}
