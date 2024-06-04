using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Mvkt.Data;
using Mvkt.Data.Models;
using Mvkt.Sync.Services;

namespace Mvkt.Sync.Protocols
{
    public abstract class ProtocolCommit
    {
        protected readonly MvktContext Db;
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

        protected async Task UpdateDelegate(Data.Models.Delegate delegat, bool staked)
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
