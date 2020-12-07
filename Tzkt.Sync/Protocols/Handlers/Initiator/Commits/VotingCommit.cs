using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Initiator
{
    class VotingCommit : ProtocolCommit
    {
        public VotingPeriod VotingPeriod { get; private set; }

        VotingCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task InitCommit(Block block)
        {
            VotingPeriod = new ProposalPeriod
            {
                Code = 0,
                Epoch = new VotingEpoch { Level = block.Level },
                Kind = VotingPeriods.Proposal,
                StartLevel = block.Level,
                EndLevel = block.Protocol.BlocksPerVoting
            };
        }

        public async Task InitRevert(Block block)
        {
            VotingPeriod = await Db.VotingPeriods.Include(x => x.Epoch).SingleAsync();
        }

        public override Task Apply()
        {
            Db.VotingEpoches.Add(VotingPeriod.Epoch);

            Db.VotingPeriods.Add(VotingPeriod);
            Cache.Periods.Add(VotingPeriod);

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            Db.VotingEpoches.Remove(VotingPeriod.Epoch);

            Db.VotingPeriods.Remove(VotingPeriod);
            Cache.Periods.Remove();

            return Task.CompletedTask;
        }

        #region static
        public static async Task<VotingCommit> Apply(ProtocolHandler proto, Block block)
        {
            var commit = new VotingCommit(proto);
            await commit.InitCommit(block);
            await commit.Apply();

            return commit;
        }

        public static async Task<VotingCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new VotingCommit(proto);
            await commit.InitRevert(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
