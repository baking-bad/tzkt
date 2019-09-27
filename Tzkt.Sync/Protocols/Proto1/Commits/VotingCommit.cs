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
    public class VotingCommit : ICommit<VotingPeriod>
    {
        #region constants
        protected virtual int BlocksPerVoting => 32_768;
        #endregion

        public VotingPeriod Content { get; protected set; }

        protected readonly TzktContext Db;
        protected readonly AccountsCache Accounts;
        protected readonly ProtocolsCache Protocols;
        protected readonly StateCache State;

        public VotingCommit(TzktContext db, CacheService cache)
        {
            Db = db;
            Accounts = cache.Accounts;
            Protocols = cache.Protocols;
            State = cache.State;
        }

        public virtual async Task<VotingCommit> Init(JToken rawBlock)
        {
            await Validate(rawBlock);
            Content = await Parse(rawBlock);
            return this;
        }

        public virtual Task<VotingCommit> Init(VotingPeriod period)
        {
            Content = period;
            return Task.FromResult(this);
        }

        public Task Apply()
        {
            if (Content != null)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public Task Revert()
        {
            if (Content != null)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public Task Validate(JToken block)
        {
            if (block["header"]["level"].Int32() >= BlocksPerVoting)
                throw new NotImplementedException();

            return Task.CompletedTask;
        }

        public async Task<VotingPeriod> Parse(JToken block)
        {
            return null;
        }
    }
}
