using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class VotingCommit : ProtocolCommit
    {
        public BlockEvents Event { get; private set; }
        public VotingPeriod Period { get; private set; }

        VotingCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawBlock rawBlock)
        {
            if (block.Events.HasFlag(BlockEvents.VotingPeriodEnd))
            {
                Event = BlockEvents.VotingPeriodEnd;
                Period = await Cache.GetCurrentVotingPeriodAsync();
                Period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == Period.EpochId);
            }
            else if (block.Events.HasFlag(BlockEvents.VotingPeriodBegin))
            {
                Event = BlockEvents.VotingPeriodBegin;
                var protocol = await Cache.GetProtocolAsync(rawBlock.Protocol);

                Period = await Cache.GetCurrentVotingPeriodAsync();
                Period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == Period.EpochId);

                Period = rawBlock.Metadata.VotingPeriod switch
                {
                    "proposal" => new ProposalPeriod
                    {
                        Epoch = new VotingEpoch { Level = rawBlock.Level },
                        Kind = VotingPeriods.Proposal,
                        StartLevel = rawBlock.Level,
                        EndLevel = rawBlock.Level + protocol.BlocksPerVoting - 1
                    },
                    "exploration" => new ExplorationPeriod
                    {
                        Epoch = Period.Epoch,
                        Kind = VotingPeriods.Exploration,
                        StartLevel = rawBlock.Level,
                        EndLevel = rawBlock.Level + protocol.BlocksPerVoting - 1
                    },
                    "testing" => new TestingPeriod
                    {
                        Epoch = Period.Epoch,
                        Kind = VotingPeriods.Testing,
                        StartLevel = rawBlock.Level,
                        EndLevel = rawBlock.Level + protocol.BlocksPerVoting - 1
                    },
                    "promotion" => new PromotionPeriod
                    {
                        Epoch = Period.Epoch,
                        Kind = VotingPeriods.Promotion,
                        StartLevel = rawBlock.Level,
                        EndLevel = rawBlock.Level + protocol.BlocksPerVoting - 1
                    },
                    _ => throw new Exception("invalid voting period")
                };
            }
        }

        public async Task Init(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.VotingPeriodEnd))
            {
                Event = BlockEvents.VotingPeriodEnd;
                Period = await Cache.GetCurrentVotingPeriodAsync();
                Period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == Period.EpochId);
            }
            else if (block.Events.HasFlag(BlockEvents.VotingPeriodBegin))
            {
                Event = BlockEvents.VotingPeriodBegin;
                Period = await Cache.GetCurrentVotingPeriodAsync();
                Period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == Period.EpochId);
            }
        }

        public override Task Apply()
        {
            if (Event == BlockEvents.VotingPeriodEnd)
            {
                #region entities
                var epoch = Period.Epoch;

                Db.TryAttach(epoch);
                #endregion

                epoch.Progress++;
            }
            else if (Event == BlockEvents.VotingPeriodBegin)
            {
                Db.VotingPeriods.Add(Period);
                Cache.AddVotingPeriod(Period);
            }

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            if (Event == BlockEvents.VotingPeriodEnd)
            {
                #region entities
                var epoch = Period.Epoch;

                Db.TryAttach(epoch);
                #endregion

                epoch.Progress--;
            }
            else if (Event == BlockEvents.VotingPeriodBegin)
            {
                if (Period.Epoch.Progress == 0)
                    Db.VotingEpoches.Remove(Period.Epoch);

                Db.VotingPeriods.Remove(Period);
                Cache.RemoveVotingPeriod();
            }

            return Task.CompletedTask;
        }

        #region static
        public static async Task<VotingCommit> Apply(ProtocolHandler proto, Block block, RawBlock rawBlock)
        {
            var commit = new VotingCommit(proto);
            await commit.Init(block, rawBlock);
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
