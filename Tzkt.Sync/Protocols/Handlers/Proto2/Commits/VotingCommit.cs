using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class VotingCommit : ProtocolCommit
    {
        public VotingCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement rawBlock)
        {
            #region init
            var events = BlockEvents.None;
            VotingPeriod period = null;

            if (block.Events.HasFlag(BlockEvents.VotingPeriodEnd))
            {
                events = BlockEvents.VotingPeriodEnd;
                period = await Cache.Periods.CurrentAsync();
                period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == period.EpochId);
            }
            else if (block.Events.HasFlag(BlockEvents.VotingPeriodBegin))
            {
                events = BlockEvents.VotingPeriodBegin;
                period = await Cache.Periods.CurrentAsync();
                period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == period.EpochId);

                period = rawBlock.Required("metadata").RequiredString("voting_period_kind") switch
                {
                    "proposal" => new ProposalPeriod
                    {
                        Code = period.Code + 1,
                        Epoch = new VotingEpoch { Level = block.Level },
                        Kind = VotingPeriods.Proposal,
                        StartLevel = block.Level,
                        EndLevel = block.Level + block.Protocol.BlocksPerVoting - 1
                    },
                    "exploration" => new ExplorationPeriod
                    {
                        Code = period.Code + 1,
                        Epoch = period.Epoch,
                        Kind = VotingPeriods.Exploration,
                        StartLevel = block.Level,
                        EndLevel = block.Level + block.Protocol.BlocksPerVoting - 1
                    },
                    "testing" => new TestingPeriod
                    {
                        Code = period.Code + 1,
                        Epoch = period.Epoch,
                        Kind = VotingPeriods.Testing,
                        StartLevel = block.Level,
                        EndLevel = block.Level + block.Protocol.BlocksPerVoting - 1
                    },
                    "promotion" => new PromotionPeriod
                    {
                        Code = period.Code + 1,
                        Epoch = period.Epoch,
                        Kind = VotingPeriods.Promotion,
                        StartLevel = block.Level,
                        EndLevel = block.Level + block.Protocol.BlocksPerVoting - 1
                    },
                    _ => throw new Exception("invalid voting period")
                };
            }
            #endregion

            if (events == BlockEvents.VotingPeriodEnd)
            {
                #region entities
                var epoch = period.Epoch;

                Db.TryAttach(epoch);
                #endregion

                epoch.Progress++;
            }
            else if (events == BlockEvents.VotingPeriodBegin)
            {
                Db.VotingPeriods.Add(period);
                Cache.Periods.Add(period);
            }
        }

        public virtual async Task Revert(Block block)
        {
            #region init
            var events = BlockEvents.None;
            VotingPeriod period = null;

            if (block.Events.HasFlag(BlockEvents.VotingPeriodEnd))
            {
                events = BlockEvents.VotingPeriodEnd;
                period = await Cache.Periods.CurrentAsync();
                period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == period.EpochId);
            }
            else if (block.Events.HasFlag(BlockEvents.VotingPeriodBegin))
            {
                events = BlockEvents.VotingPeriodBegin;
                period = await Cache.Periods.CurrentAsync();
                period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == period.EpochId);
            }
            #endregion

            if (events == BlockEvents.VotingPeriodEnd)
            {
                #region entities
                var epoch = period.Epoch;

                Db.TryAttach(epoch);
                #endregion

                epoch.Progress--;
            }
            else if (events == BlockEvents.VotingPeriodBegin)
            {
                if (period.Epoch.Progress == 0)
                    Db.VotingEpoches.Remove(period.Epoch);

                Db.VotingPeriods.Remove(period);
                Cache.Periods.Remove();
            }
        }

        public override Task Apply() => Task.CompletedTask;
        public override Task Revert() => Task.CompletedTask;
    }
}
