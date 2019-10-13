using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Initiator
{
    class VotingCommit : ProtocolCommit
    {
        #region constants
        const int BlocksPerVotingPeriod = 32768;
        #endregion

        public VotingPeriod VotingPeriod { get; private set; }

        public VotingCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init()
        {
            VotingPeriod = await Db.VotingPeriods.Include(x => x.Epoch).SingleAsync();
        }

        public override Task Init(IBlock block)
        {
            VotingPeriod = new ProposalPeriod
            {
                Epoch = new VotingEpoch { Level = block.Level },
                Kind = VotingPeriods.Proposal,
                StartLevel = block.Level,
                EndLevel = BlocksPerVotingPeriod
            };
            return Task.CompletedTask;
        }

        public override Task Apply()
        {
            if (VotingPeriod == null)
                throw new Exception("Commit is not initialized");

            Db.VotingEpoches.Add(VotingPeriod.Epoch);
            Db.VotingPeriods.Add(VotingPeriod);
            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            if (VotingPeriod == null)
                throw new Exception("Commit is not initialized");

            Db.VotingEpoches.Remove(VotingPeriod.Epoch);
            Db.VotingPeriods.Remove(VotingPeriod);
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
