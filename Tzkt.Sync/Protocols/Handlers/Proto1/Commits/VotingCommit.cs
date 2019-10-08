using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    class VotingCommit : ProtocolCommit
    {
        public VotingPeriod VotingPeriod { get; protected set; }

        public VotingCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override Task Init(IBlock block)
        {
            return Task.CompletedTask;
        }

        public override Task Apply()
        {
            if (VotingPeriod != null)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            if (VotingPeriod != null)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        #region static
        public static async Task<VotingCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new VotingCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static Task<VotingCommit> Create(ProtocolHandler protocol, List<ICommit> commits, VotingPeriod votingPeriod)
        {
            var commit = new VotingCommit(protocol, commits) { VotingPeriod = votingPeriod };
            return Task.FromResult(commit);
        }
        #endregion
    }
}
