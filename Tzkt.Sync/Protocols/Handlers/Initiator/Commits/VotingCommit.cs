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

        public async Task Init(RawBlock rawBlock)
        {
            var protocol = await Cache.GetProtocolAsync(rawBlock.Protocol);

            VotingPeriod = new ProposalPeriod
            {
                Code = 0,
                Epoch = new VotingEpoch { Level = rawBlock.Level },
                Kind = VotingPeriods.Proposal,
                StartLevel = rawBlock.Level,
                EndLevel = protocol.BlocksPerVoting
            };
        }

        public async Task Init(Block block)
        {
            VotingPeriod = await Db.VotingPeriods.Include(x => x.Epoch).SingleAsync();
        }

        public override Task Apply()
        {
            Db.VotingEpoches.Add(VotingPeriod.Epoch);

            Db.VotingPeriods.Add(VotingPeriod);
            Cache.AddVotingPeriod(VotingPeriod);

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            Db.VotingEpoches.Remove(VotingPeriod.Epoch);

            Db.VotingPeriods.Remove(VotingPeriod);
            Cache.RemoveVotingPeriod();

            return Task.CompletedTask;
        }

        #region static
        public static async Task<VotingCommit> Apply(ProtocolHandler proto, RawBlock rawBlock)
        {
            var commit = new VotingCommit(proto);
            await commit.Init(rawBlock);
            await commit.Apply();

            return commit;
        }

        public static async Task<VotingCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new VotingCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
