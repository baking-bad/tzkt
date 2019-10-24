using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class VotingCommit : ProtocolCommit
    {
        public VotingPeriod CurrentPeriod { get; private set; }
        public VotingPeriod NextPeriod { get; private set; }

        VotingCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(RawBlock rawBlock)
        {
            CurrentPeriod = await Cache.GetCurrentVotingPeriodAsync();

            if (CurrentPeriod.EndLevel >= rawBlock.Level)
            {
                NextPeriod = CurrentPeriod;
            }
            else
            {
                var protocol = await Cache.GetProtocolAsync(rawBlock.Protocol);
                CurrentPeriod.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == CurrentPeriod.EpochId);
                NextPeriod = rawBlock.Metadata.VotingPeriod switch
                {
                    "proposal" => new ProposalPeriod
                    {
                        Epoch = new VotingEpoch { Level = rawBlock.Level },
                        Kind = VotingPeriods.Proposal,
                        StartLevel = rawBlock.Level,
                        EndLevel = protocol.BlocksPerVoting
                    },
                    "exploration" => new ExplorationPeriod
                    {
                        Epoch = CurrentPeriod.Epoch,
                        Kind = VotingPeriods.Exploration,
                        StartLevel = rawBlock.Level,
                        EndLevel = protocol.BlocksPerVoting
                    },
                    "testing" => new TestingPeriod
                    {
                        Epoch = CurrentPeriod.Epoch,
                        Kind = VotingPeriods.Testing,
                        StartLevel = rawBlock.Level,
                        EndLevel = protocol.BlocksPerVoting
                    },
                    "promotion" => new PromotionPeriod
                    {
                        Epoch = CurrentPeriod.Epoch,
                        Kind = VotingPeriods.Promotion,
                        StartLevel = rawBlock.Level,
                        EndLevel = protocol.BlocksPerVoting
                    },
                    _ => throw new Exception("invalid voting period")
                };
            }
        }

        public async Task Init(Block block)
        {
            CurrentPeriod = await Cache.GetCurrentVotingPeriodAsync();

            if (CurrentPeriod.StartLevel == block.Level)
            {
                NextPeriod = await Db.VotingPeriods.FirstOrDefaultAsync(x => x.EndLevel == block.Level - 1);
            }
            else
            {
                NextPeriod = CurrentPeriod;
            }
        }

        public override Task Apply()
        {
            if (CurrentPeriod != NextPeriod)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            if (CurrentPeriod != NextPeriod)
            {
                throw new NotImplementedException();
            }

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
