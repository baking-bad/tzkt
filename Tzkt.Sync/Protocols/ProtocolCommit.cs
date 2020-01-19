using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols
{
    public abstract class ProtocolCommit : ICommit
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

        public abstract Task Apply();

        public abstract Task Revert();

        public async Task Spend(Account account, long amount)
        {
            account.Balance -= amount;

            if (account.Balance <= 0 && account.Type == AccountType.User)
            {
                account.Counter = (await Cache.GetAppStateAsync()).ManagerCounter;
                (account as User).Revealed = false;
            }
        }

        public Task Return(Account account, long amount, bool reveal = false)
        {
            account.Balance += amount;

            if (account.Balance <= amount && account.Type == AccountType.User && !reveal)
                (account as User).Revealed = true;

            return Task.CompletedTask;
        }
    }
}
