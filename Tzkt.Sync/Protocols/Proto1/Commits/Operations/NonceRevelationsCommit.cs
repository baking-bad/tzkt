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
    public class NonceRevelationsCommit : ICommit<List<NonceRevelationOperation>>
    {
        public List<NonceRevelationOperation> Content { get; protected set; }

        protected readonly TzktContext Db;
        protected readonly AccountsCache Accounts;
        protected readonly ProtocolsCache Protocols;
        protected readonly StateCache State;

        public NonceRevelationsCommit(TzktContext db, CacheService cache)
        {
            Db = db;
            Accounts = cache.Accounts;
            Protocols = cache.Protocols;
            State = cache.State;
        }

        public virtual async Task<NonceRevelationsCommit> Init(JToken rawBlock, Block parsedBlock)
        {
            await Validate(rawBlock);
            Content = await Parse(rawBlock, parsedBlock);
            return this;
        }

        public virtual Task<NonceRevelationsCommit> Init(List<NonceRevelationOperation> operations)
        {
            Content = operations;
            return Task.FromResult(this);
        }

        public Task Apply()
        {
            foreach (var revelation in Content)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public Task Revert()
        {
            foreach (var revelation in Content)
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
                    .Where(x => (x["kind"]?.String() ?? throw new Exception("Invalid content kind")) == "seed_nonce_revelation"))
                {
                    throw new NotImplementedException();
                }
            }

            return Task.CompletedTask;
        }

        public async Task<List<NonceRevelationOperation>> Parse(JToken rawBlock, Block parsedBlock)
        {
            var result = new List<NonceRevelationOperation>();

            foreach (var operation in rawBlock["operations"][2])
            {
                var opHash = operation["hash"].String();

                foreach (var content in operation["contents"].Where(x => x["kind"].String() == "seed_nonce_revelation"))
                {
                    var metadata = content["metadata"];

                    throw new NotImplementedException();
                }
            }

            return result;
        }
    }
}
