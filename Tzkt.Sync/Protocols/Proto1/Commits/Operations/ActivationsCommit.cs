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
    public class ActivationsCommit : ICommit<List<ActivationOperation>>
    {
        public List<ActivationOperation> Content { get; protected set; }

        protected readonly TzktContext Db;
        protected readonly AccountsCache Accounts;
        protected readonly ProtocolsCache Protocols;
        protected readonly StateCache State;

        public ActivationsCommit(TzktContext db, CacheService cache)
        {
            Db = db;
            Accounts = cache.Accounts;
            Protocols = cache.Protocols;
            State = cache.State;
        }

        public virtual async Task<ActivationsCommit> Init(JToken rawBlock, Block parsedBlock)
        {
            await Validate(rawBlock);
            Content = await Parse(rawBlock, parsedBlock);
            return this;
        }

        public virtual Task<ActivationsCommit> Init(List<ActivationOperation> operations)
        {
            Content = operations;
            return Task.FromResult(this);
        }

        public Task Apply()
        {
            foreach (var activation in Content)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public Task Revert()
        {
            foreach (var activation in Content)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public Task Validate(JToken block)
        {
            foreach (var operation in block["operations"]?[2] ?? throw new Exception("Anonimous operations missed"))
            {
                var opHash = operation["hash"]?.String();
                if (String.IsNullOrEmpty(opHash))
                    throw new Exception($"Invalid anonimous operation hash '{opHash}'");

                foreach (var content in operation["contents"]
                    .Where(x => (x["kind"]?.String() ?? throw new Exception("Invalid content kind")) == "activation"))
                {
                    throw new NotImplementedException();
                }
            }

            return Task.CompletedTask;
        }

        public async Task<List<ActivationOperation>> Parse(JToken rawBlock, Block parsedBlock)
        {
            var result = new List<ActivationOperation>();

            foreach (var operation in rawBlock["operations"][2])
            {
                var opHash = operation["hash"].String();

                foreach (var content in operation["contents"].Where(x => x["kind"].String() == "activation"))
                {
                    var metadata = content["metadata"];

                    throw new NotImplementedException();
                }
            }

            return result;
        }
    }
}
