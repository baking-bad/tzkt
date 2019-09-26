using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    public class FreezerCommit : ICommit<List<IBalanceUpdate>>
    {
        #region constants
        protected virtual int BlocksPerCycle => 4096;
        #endregion

        public List<IBalanceUpdate> Content { get; protected set; }

        protected readonly TzktContext Db;
        protected readonly AccountsCache Accounts;
        protected readonly ProtocolsCache Protocols;
        protected readonly StateCache State;

        public FreezerCommit(TzktContext db, CacheService cache)
        {
            Db = db;
            Accounts = cache.Accounts;
            Protocols = cache.Protocols;
            State = cache.State;
        }

        public virtual async Task<FreezerCommit> Init(JToken rawBlock)
        {
            await Validate(rawBlock);
            Content = await Parse(rawBlock);
            return this;
        }

        public virtual Task<FreezerCommit> Init(List<IBalanceUpdate> updates)
        {
            Content = updates;
            return Task.FromResult(this);
        }

        public Task Apply()
        {
            foreach (var update in Content)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public Task Revert()
        {
            foreach (var update in Content)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public Task Validate(JToken block)
        {
            var updates = BalanceUpdates.Parse((JArray)block["metadata"]?["balance_updates"]
                ?? throw new Exception("Invalid block balance updates"));

            if (updates.Count > 2)
            {
                if (block["header"]["level"].Int32() % BlocksPerCycle != 0)
                    throw new Exception("Unexpected freezer updates");

                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public Task<List<IBalanceUpdate>> Parse(JToken block)
        {
            var updates = BalanceUpdates.Parse((JArray)block["metadata"]?["balance_updates"]
                ?? throw new Exception("Invalid block balance updates"));

            return Task.FromResult(updates.Skip(2).ToList());
        }
    }
}
