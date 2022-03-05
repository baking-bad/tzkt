using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols
{
    public abstract class ProtocolCommit
    {
        protected readonly TzktContext Db;
        protected readonly CacheService Cache;
        protected readonly ProtocolHandler Proto;
        protected readonly ILogger Logger;

        public ProtocolCommit(ProtocolHandler protocol)
        {
            Proto = protocol;
            Db = protocol.Db;
            Cache = protocol.Cache;
            Logger = protocol.Logger;
        }

        public Task Spend(Account account, long amount)
        {
            account.Balance -= amount;

            if (account.Balance <= 0 && account.Type == AccountType.User)
            {
                account.Counter = Cache.AppState.GetManagerCounter();
                (account as User).Revealed = false;
            }

            return Task.CompletedTask;
        }

        public Task Return(Account account, long amount, bool reveal = false)
        {
            account.Balance += amount;

            if (account.Balance <= amount && account.Type == AccountType.User && !reveal)
                (account as User).Revealed = true;

            return Task.CompletedTask;
        }

        protected async Task UpdateDelegate(Delegate delegat, bool staked)
        {
            delegat.Staked = staked;
            foreach (var delegator in await Db.Accounts.Where(x => x.DelegateId == delegat.Id).ToListAsync())
            {
                Cache.Accounts.Add(delegator);
                delegator.Staked = staked;
            }
        }
    }
}
