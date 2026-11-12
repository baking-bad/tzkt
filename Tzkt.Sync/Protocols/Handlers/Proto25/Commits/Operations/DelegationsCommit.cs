using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto25
{
    class DelegationsCommit(ProtocolHandler protocol) : Proto18.DelegationsCommit(protocol)
    {
        protected override async Task Unstake(DelegationOperation operation, List<JsonElement> balanceUpdates)
        {
            await base.Unstake(operation, balanceUpdates);

            #region debug
            if (await Cache.Accounts.GetAsync(operation.SenderId) is User { StakedPseudotokens: not null } sender)
                throw new Exception($"Staking pseudotokens left after delegation change {sender.Address}");
            #endregion
        }
    }
}
