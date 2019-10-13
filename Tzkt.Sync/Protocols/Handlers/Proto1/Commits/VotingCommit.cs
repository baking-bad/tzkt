using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class VotingCommit : ProtocolCommit
    {
        public VotingPeriod VotingPeriod { get; protected set; }

        public VotingCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init()
        {
            var block = await Cache.GetCurrentBlockAsync();
            VotingPeriod = await Db.VotingPeriods.Include(x => x.Epoch).Where(x => x.StartLevel == block.Level).FirstOrDefaultAsync();
        }

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

        public static async Task<VotingCommit> Create(ProtocolHandler protocol, List<ICommit> commits)
        {
            var commit = new VotingCommit(protocol, commits);
            await commit.Init();
            return commit;
        }
        #endregion
    }
}
