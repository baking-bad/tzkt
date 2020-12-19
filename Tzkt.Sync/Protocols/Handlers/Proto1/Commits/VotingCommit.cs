using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class VotingCommit : ProtocolCommit
    {
        public VotingCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement rawBlock)
        {
            var state = Cache.AppState.Get();
            var current = await Cache.Periods.GetAsync(state.VotingPeriod);

            if (block.Level != current.LastLevel) return;

            Db.TryAttach(current);
            current.Status = GetPeriodStatus(current);
                
            await UpdateProposalsStatus(current);

            var nextPeriod = current.Status == PeriodStatus.Success
                ? StartNextPeriod(block, current)
                : StartProposalPeriod(block, current);

            state.VotingPeriod = nextPeriod.Index;
            state.VotingEpoch = nextPeriod.Epoch;
        }

        public virtual async Task Revert(Block block)
        {
            var state = Cache.AppState.Get();
            var current = await Cache.Periods.GetAsync(state.VotingPeriod);

            if (block.Level != current.FirstLevel - 1) return;

            var prev = await Cache.Periods.GetAsync(state.VotingPeriod - 1);

            Db.TryAttach(prev);
            prev.Status = PeriodStatus.Active;

            if (prev.Kind != PeriodKind.Proposal || prev.ProposalsCount > 0)
            {
                var proposals = await Db.Proposals
                    .Where(x => x.LastPeriod >= prev.Index)
                    .ToListAsync();

                foreach (var proposal in proposals)
                {
                    proposal.Status = ProposalStatus.Active;
                    proposal.LastPeriod = prev.Index;
                }
            }

            await Db.Database.ExecuteSqlRawAsync($@"
                DELETE FROM ""VotingPeriods"" WHERE ""Index"" = {current.Index};
                DELETE FROM ""VotingSnapshots"" WHERE ""Period"" = {current.Index};");
            Cache.Periods.Remove(current);
                
            state.VotingPeriod = prev.Index;
            state.VotingEpoch = prev.Epoch;
        }

        protected PeriodStatus GetPeriodStatus(VotingPeriod p)
        {
            if (p.Kind == PeriodKind.Proposal)
            {
                if (p.ProposalsCount == 0)
                    return PeriodStatus.NoProposals;

                if (p.TopRolls < p.TotalRolls * p.UpvotesQuorum / 10000)
                    return PeriodStatus.NoQuorum;
            }
            else if (p.Kind == PeriodKind.Exploration || p.Kind == PeriodKind.Promotion)
            {
                if (p.YayRolls + p.NayRolls + p.PassRolls < p.TotalRolls * p.BallotsQuorum / 10000)
                    return PeriodStatus.NoQuorum;

                if (p.YayRolls < (p.YayRolls + p.NayRolls) * p.Supermajority / 10000)
                    return PeriodStatus.NoSupermajority;
            }

            return PeriodStatus.Success;
        }

        protected async Task UpdateProposalsStatus(VotingPeriod p)
        {
            if (p.Kind == PeriodKind.Proposal)
            {
                if (p.Status == PeriodStatus.NoProposals) return;

                var proposals = await Db.Proposals
                    .Where(x => x.Status == ProposalStatus.Active)
                    .ToListAsync();

                foreach (var proposal in proposals)
                    proposal.Status = ProposalStatus.Skipped;

                if (p.Status == PeriodStatus.Success)
                {
                    var winner = proposals.First(x => x.Rolls == p.TopRolls);
                    winner.Status = GetProposalStatus(winner, p);
                    if (winner.Status == ProposalStatus.Active)
                        winner.LastPeriod = p.Index + 1;
                }
            }
            else
            {
                var proposal = await Db.Proposals.FirstAsync(x => x.Status == ProposalStatus.Active);
                proposal.Status = GetProposalStatus(proposal, p);
                if (proposal.Status == ProposalStatus.Active)
                    proposal.LastPeriod = p.Index + 1;
            }
        }

        protected virtual ProposalStatus GetProposalStatus(Proposal proposal, VotingPeriod period)
        {
            if (period.Status == PeriodStatus.Success)
                return period.Kind == PeriodKind.Promotion
                    ? ProposalStatus.Accepted
                    : ProposalStatus.Active;

            if (period.Status == PeriodStatus.NoSupermajority)
                return ProposalStatus.Rejected;

            return ProposalStatus.Skipped;
        }

        protected virtual VotingPeriod StartNextPeriod(Block block, VotingPeriod current)
        {
            switch (current.Kind)
            {
                case PeriodKind.Proposal:
                    return StartBallotPeriod(block, current, PeriodKind.Exploration);
                case PeriodKind.Exploration:
                    return StartWaitingPeriod(block, current, PeriodKind.Testing);
                case PeriodKind.Testing:
                    return StartBallotPeriod(block, current, PeriodKind.Promotion);
                case PeriodKind.Promotion:
                    return StartProposalPeriod(block, current);
                default:
                    throw new Exception("Invalid voting period kind");
            }
        }

        protected VotingPeriod StartProposalPeriod(Block block, VotingPeriod current)
        {
            var proto = block.Protocol;
            var period = new VotingPeriod
            {
                Index = current.Index + 1,
                Epoch = current.Epoch + 1,
                FirstLevel = block.Level + 1,
                LastLevel = block.Level + proto.BlocksPerVoting,
                Kind = PeriodKind.Proposal,
                Status = PeriodStatus.Active
            };

            #region snapshot
            var snapshots = Cache.Accounts.GetDelegates()
                .Where(x => x.Staked && x.StakingBalance >= proto.TokensPerRoll)
                .Select(x => new VotingSnapshot
                {
                    Level = block.Level,
                    Period = period.Index,
                    BakerId = x.Id,
                    Rolls = (int)(x.StakingBalance / proto.TokensPerRoll),
                    Status = VoterStatus.None
                });

            period.TotalBakers = snapshots.Count();
            period.TotalRolls = snapshots.Sum(x => x.Rolls);
            #endregion

            #region quorum
            period.UpvotesQuorum = proto.ProposalQuorum;
            period.ProposalsCount = 0;
            period.TopUpvotes = 0;
            period.TopRolls = 0;
            #endregion

            Db.VotingSnapshots.AddRange(snapshots);
            Db.VotingPeriods.Add(period);
            Cache.Periods.Add(period);
            return period;
        }

        protected VotingPeriod StartBallotPeriod(Block block, VotingPeriod current, PeriodKind kind)
        {
            var proto = block.Protocol;
            var period = new VotingPeriod
            {
                Index = current.Index + 1,
                Epoch = current.Epoch,
                FirstLevel = block.Level + 1,
                LastLevel = block.Level + proto.BlocksPerVoting,
                Kind = kind,
                Status = PeriodStatus.Active
            };

            #region snapshot
            var snapshots = Cache.Accounts.GetDelegates()
                .Where(x => x.Staked && x.StakingBalance >= proto.TokensPerRoll)
                .Select(x => new VotingSnapshot
                {
                    Level = block.Level,
                    Period = period.Index,
                    BakerId = x.Id,
                    Rolls = (int)(x.StakingBalance / proto.TokensPerRoll),
                    Status = VoterStatus.None
                });

            period.TotalBakers = snapshots.Count();
            period.TotalRolls = snapshots.Sum(x => x.Rolls);
            #endregion

            #region quorum
            period.ParticipationEma = GetParticipationEma(period, proto);
            period.BallotsQuorum = GetBallotQuorum(period, proto);
            period.Supermajority = 8000;

            period.YayBallots = 0;
            period.YayRolls = 0;
            period.NayBallots = 0;
            period.NayRolls = 0;
            period.PassBallots = 0;
            period.PassRolls = 0;
            #endregion

            Db.VotingSnapshots.AddRange(snapshots);
            Db.VotingPeriods.Add(period);
            Cache.Periods.Add(period);
            return period;
        }

        protected VotingPeriod StartWaitingPeriod(Block block, VotingPeriod current, PeriodKind kind)
        {
            var proto = block.Protocol;
            var period = new VotingPeriod
            {
                Index = current.Index + 1,
                Epoch = current.Epoch,
                FirstLevel = block.Level + 1,
                LastLevel = block.Level + proto.BlocksPerVoting,
                Kind = kind,
                Status = PeriodStatus.Active
            };

            Db.VotingPeriods.Add(period);
            Cache.Periods.Add(period);
            return period;
        }

        protected virtual int GetParticipationEma(VotingPeriod period, Protocol proto)
        {
            var prev = Db.VotingPeriods
                .AsNoTracking()
                .OrderByDescending(x => x.Index)
                .FirstOrDefault(x => x.Kind == PeriodKind.Exploration || x.Kind == PeriodKind.Promotion);

            if (prev != null)
            {
                var participation = 10000 * (prev.YayRolls + prev.NayRolls + prev.PassRolls) / prev.TotalRolls;
                return ((int)prev.ParticipationEma * 8000 + (int)participation * 2000) / 10000;
            }

            return 8000;
        }

        protected virtual int GetBallotQuorum(VotingPeriod period, Protocol proto)
        {
            var prev = Db.VotingPeriods
                .AsNoTracking()
                .OrderByDescending(x => x.Index)
                .FirstOrDefault(x => x.Kind == PeriodKind.Exploration || x.Kind == PeriodKind.Promotion);

            if (prev != null)
            {
                var participation = 10000 * (prev.YayRolls + prev.NayRolls + prev.PassRolls) / prev.TotalRolls;
                return ((int)prev.BallotsQuorum * 8000 + (int)participation * 2000) / 10000;
            }

            return 8000;
        }
    }
}
