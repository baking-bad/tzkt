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
    public class BallotsCommit : ICommit<List<BallotOperation>>
    {
        public List<BallotOperation> Content { get; protected set; }

        protected readonly TzktContext Db;
        protected readonly AccountsCache Accounts;
        protected readonly ProtocolsCache Protocols;
        protected readonly StateCache State;

        public BallotsCommit(TzktContext db, CacheService cache)
        {
            Db = db;
            Accounts = cache.Accounts;
            Protocols = cache.Protocols;
            State = cache.State;
        }

        public virtual async Task<BallotsCommit> Init(JToken rawBlock, Block parsedBlock)
        {
            await Validate(rawBlock);
            Content = await Parse(rawBlock, parsedBlock);
            return this;
        }

        public virtual Task<BallotsCommit> Init(List<BallotOperation> operations)
        {
            Content = operations;
            return Task.FromResult(this);
        }

        public Task Apply()
        {
            foreach (var ballot in Content)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public Task Revert()
        {
            foreach (var ballot in Content)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public Task Validate(JToken block)
        {
            foreach (var operation in block["operations"]?[1] ?? throw new Exception("Voting operations missed"))
            {
                var opHash = operation["hash"]?.String();
                if (String.IsNullOrEmpty(opHash))
                    throw new Exception($"Invalid voting operation hash '{opHash}'");

                foreach (var content in operation["contents"]
                    .Where(x => (x["kind"]?.String() ?? throw new Exception("Invalid content kind")) == "ballot"))
                {
                    throw new NotImplementedException();
                }
            }

            return Task.CompletedTask;
        }

        public async Task<List<BallotOperation>> Parse(JToken rawBlock, Block parsedBlock)
        {
            var result = new List<BallotOperation>();

            foreach (var operation in rawBlock["operations"][1])
            {
                var opHash = operation["hash"].String();

                foreach (var content in operation["contents"].Where(x => x["kind"].String() == "ballot"))
                {
                    var metadata = content["metadata"];

                    throw new NotImplementedException();
                }
            }

            return result;
        }
    }
}
