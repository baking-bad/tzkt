using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols
{
    public abstract class ProtocolCommit(ProtocolHandler protocol)
    {
        protected readonly TzktContext Db = protocol.Db;
        protected readonly CacheService Cache = protocol.Cache;
        protected readonly ProtocolHandler Proto = protocol;
        protected readonly BlockContext Context = protocol.Context;
        protected readonly ILogger Logger = protocol.Logger;

        protected void PayFee(Account account, long bakerFee)
        {
            Spend(account, bakerFee);
            Context.Block.Fees += bakerFee;
            Cache.Statistics.Current.TotalBurned += bakerFee;
        }

        protected void RevertPayFee(Account account, long bakerFee)
        {
            RevertSpend(account, bakerFee);
        }

        protected void Spend(Account account, long amount)
        {
            account.Balance -= amount;
        }

        protected void RevertSpend(Account account, long amount)
        {
            account.Balance += amount;
        }

        protected void Receive(Account account, long amount)
        {
            account.Balance += amount;
        }

        protected void RevertReceive(Account account, long amount)
        {
            account.Balance -= amount;
        }
    }
}
