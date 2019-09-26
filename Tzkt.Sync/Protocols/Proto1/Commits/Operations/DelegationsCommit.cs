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
    public class DelegationsCommit : ICommit<List<DelegationOperation>>
    {
        public List<DelegationOperation> Content { get; protected set; }

        protected readonly TzktContext Db;
        protected readonly AccountsCache Accounts;
        protected readonly ProtocolsCache Protocols;
        protected readonly StateCache State;

        public DelegationsCommit(TzktContext db, CacheService cache)
        {
            Db = db;
            Accounts = cache.Accounts;
            Protocols = cache.Protocols;
            State = cache.State;
        }

        public virtual async Task<DelegationsCommit> Init(JToken rawBlock, Block parsedBlock)
        {
            await Validate(rawBlock);
            Content = await Parse(rawBlock, parsedBlock);
            return this;
        }

        public virtual Task<DelegationsCommit> Init(List<DelegationOperation> operations)
        {
            Content = operations;
            return Task.FromResult(this);
        }

        public Task Apply()
        {
            foreach (var delegation in Content)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public Task Revert()
        {
            foreach (var delegation in Content)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public Task Validate(JToken block)
        {
            foreach (var operation in block["operations"]?[3] ?? throw new Exception("Manager operations missed"))
            {
                var opHash = operation["hash"]?.String();
                if (String.IsNullOrEmpty(opHash))
                    throw new Exception($"Invalid manager operation hash '{opHash}'");

                foreach (var content in operation["contents"]
                    .Where(x => (x["kind"]?.String() ?? throw new Exception("Invalid content kind")) == "delegation"))
                {
                    throw new NotImplementedException();
                }
            }

            return Task.CompletedTask;
        }

        public async Task<List<DelegationOperation>> Parse(JToken rawBlock, Block parsedBlock)
        {
            var result = new List<DelegationOperation>();

            foreach (var operation in rawBlock["operations"][3])
            {
                var opHash = operation["hash"].String();

                foreach (var content in operation["contents"].Where(x => x["kind"].String() == "delegation"))
                {
                    var metadata = content["metadata"];

                    throw new NotImplementedException();
                }
            }

            return result;
        }
    }
}
