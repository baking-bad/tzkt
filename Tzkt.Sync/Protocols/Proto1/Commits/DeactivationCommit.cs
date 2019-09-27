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
    public class DeactivationCommit : ICommit<List<Data.Models.Delegate>>
    {
        #region constants
        protected virtual int BlocksPerCycle => 4096;
        #endregion

        public List<Data.Models.Delegate> Content { get; protected set; }

        protected readonly TzktContext Db;
        protected readonly AccountsCache Accounts;
        protected readonly ProtocolsCache Protocols;
        protected readonly StateCache State;

        public DeactivationCommit(TzktContext db, CacheService cache)
        {
            Db = db;
            Accounts = cache.Accounts;
            Protocols = cache.Protocols;
            State = cache.State;
        }

        public virtual async Task<DeactivationCommit> Init(JToken rawBlock)
        {
            await Validate(rawBlock);
            Content = await Parse(rawBlock);
            return this;
        }

        public virtual Task<DeactivationCommit> Init(List<Data.Models.Delegate> bakers)
        {
            Content = bakers;
            return Task.FromResult(this);
        }

        public Task Apply()
        {
            foreach (var baker in Content)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public Task Revert()
        {
            foreach (var baker in Content)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public async Task Validate(JToken block)
        {
            foreach (var baker in block["metadata"]?["deactivated"]?.ToObject<List<string>>() ?? new List<string>())
            {
                if (!await Accounts.ExistsAsync(baker, AccountType.Delegate))
                    throw new Exception($"Invalid deactivated baker {baker}");
            }
        }

        public async Task<List<Data.Models.Delegate>> Parse(JToken block)
        {
            var deactivated = block["metadata"]?["deactivated"]?.ToObject<List<string>>() ?? new List<string>();
            var result = new List<Data.Models.Delegate>(deactivated.Count);

            foreach (var baker in deactivated)
                result.Add((Data.Models.Delegate)await Accounts.GetAccountAsync(baker));

            return result;
        }
    }
}
