﻿using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class VotingCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement rawBlock)
        {
            var state = Cache.AppState.Get();
            var current = await Cache.Periods.GetAsync(state.VotingPeriod);

            if (block.Level != current.LastLevel) return;

            Db.TryAttach(current);
            current.Status = GetPeriodStatus(current);
                
            await UpdateProposalsStatus(current);

            var nextPeriod = current.Status == PeriodStatus.Success
                ? StartNextPeriod(block, Context.Protocol, current)
                : StartProposalPeriod(block, Context.Protocol, current);

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

            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "VotingPeriods" WHERE "Index" = {0};
                DELETE FROM "VotingSnapshots" WHERE "Period" = {0};
                """, current.Index);
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

                if (p.TopVotingPower < p.TotalVotingPower * p.UpvotesQuorum / 10000)
                    return PeriodStatus.NoQuorum;

                if (p.SingleWinner == false)
                    return PeriodStatus.NoSingleWinner;
            }
            else if (p.Kind == PeriodKind.Exploration || p.Kind == PeriodKind.Promotion)
            {
                if (p.YayVotingPower + p.NayVotingPower + p.PassVotingPower < p.TotalVotingPower * p.BallotsQuorum / 10000)
                    return PeriodStatus.NoQuorum;

                if (p.YayVotingPower < (p.YayVotingPower + p.NayVotingPower) * p.Supermajority / 10000)
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

                var pendings = Db.ChangeTracker.Entries()
                    .Where(x => x.Entity is Proposal p && p.Status == ProposalStatus.Active)
                    .Select(x => (x.Entity as Proposal)!)
                    .ToList();

                foreach (var pending in pendings)
                    if (!proposals.Any(x => x.Id == pending.Id))
                        proposals.Add(pending);

                foreach (var proposal in proposals)
                    proposal.Status = ProposalStatus.Skipped;

                if (p.Status == PeriodStatus.Success)
                {
                    var winner = proposals.First(x => x.VotingPower == p.TopVotingPower);
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

        protected virtual VotingPeriod StartNextPeriod(Block block, Protocol protocol, VotingPeriod current)
        {
            return current.Kind switch
            {
                PeriodKind.Proposal => StartBallotPeriod(block, protocol, current, PeriodKind.Exploration),
                PeriodKind.Exploration => StartWaitingPeriod(block, protocol, current, PeriodKind.Testing),
                PeriodKind.Testing => StartBallotPeriod(block, protocol, current, PeriodKind.Promotion),
                PeriodKind.Promotion => StartProposalPeriod(block, protocol, current),
                _ => throw new Exception("Invalid voting period kind")
            };
        }

        protected VotingPeriod StartProposalPeriod(Block block, Protocol protocol, VotingPeriod current)
        {
            var period = new VotingPeriod
            {
                Id = 0,
                Index = current.Index + 1,
                Epoch = current.Epoch + 1,
                FirstLevel = block.Level + 1,
                LastLevel = block.Level + protocol.BlocksPerVoting,
                Kind = PeriodKind.Proposal,
                Status = PeriodStatus.Active
            };

            #region snapshot
            var snapshots = Cache.Accounts.GetDelegates()
                .Where(x => BakerIsListed(x, block, protocol))
                .Select(x => new VotingSnapshot
                {
                    Id = 0,
                    Level = block.Level,
                    Period = period.Index,
                    BakerId = x.Id,
                    VotingPower = GetVotingPower(x, block, protocol),
                    Status = VoterStatus.None
                });

            period.TotalBakers = snapshots.Count();
            period.TotalVotingPower = snapshots.Sum(x => x.VotingPower);
            #endregion

            //#region temporary diagnostics
            //var listing = Proto.Node.GetAsync($"chains/main/blocks/{block.Level}/context/raw/json/votes/listings?depth=1").Result
            //    .EnumerateArray()
            //    .ToDictionary(x => x[0].RequiredString(), x => x[1].RequiredInt64());

            //if (snapshots.Count() != listing.Count)
            //    throw new Exception("Wrong voting snapshots");

            //foreach (var snapshot in snapshots)
            //    if (!listing.TryGetValue(Cache.Accounts.GetDelegate(snapshot.BakerId).Address, out var votingPower) || snapshot.VotingPower != votingPower)
            //        throw new Exception("Wrong voting snapshot");
            //#endregion

            #region quorum
            period.UpvotesQuorum = protocol.ProposalQuorum;
            period.ProposalsCount = 0;
            period.TopUpvotes = 0;
            period.TopVotingPower = 0;
            period.SingleWinner = false;
            #endregion

            Db.VotingSnapshots.AddRange(snapshots);
            Db.VotingPeriods.Add(period);
            Cache.Periods.Add(period);
            return period;
        }

        protected VotingPeriod StartBallotPeriod(Block block, Protocol protocol, VotingPeriod current, PeriodKind kind)
        {
            var period = new VotingPeriod
            {
                Id = 0,
                Index = current.Index + 1,
                Epoch = current.Epoch,
                FirstLevel = block.Level + 1,
                LastLevel = block.Level + protocol.BlocksPerVoting,
                Kind = kind,
                Status = PeriodStatus.Active
            };

            #region snapshot
            var snapshots = Cache.Accounts.GetDelegates()
                .Where(x => BakerIsListed(x, block, protocol))
                .Select(x => new VotingSnapshot
                {
                    Id = 0,
                    Level = block.Level,
                    Period = period.Index,
                    BakerId = x.Id,
                    VotingPower = GetVotingPower(x, block, protocol),
                    Status = VoterStatus.None
                });

            period.TotalBakers = snapshots.Count();
            period.TotalVotingPower = snapshots.Sum(x => x.VotingPower);
            #endregion

            //#region temporary diagnostics
            //var listing = Proto.Node.GetAsync($"chains/main/blocks/{block.Level}/context/raw/json/votes/listings?depth=1").Result
            //    .EnumerateArray()
            //    .ToDictionary(x => x[0].RequiredString(), x => x[1].RequiredInt64());

            //if (snapshots.Count() != listing.Count)
            //    throw new Exception("Wrong voting snapshots");

            //foreach (var snapshot in snapshots)
            //    if (!listing.TryGetValue(Cache.Accounts.GetDelegate(snapshot.BakerId).Address, out var votingPower) || snapshot.VotingPower != votingPower)
            //        throw new Exception("Wrong voting snapshot");
            //#endregion

            #region quorum
            period.ParticipationEma = GetParticipationEma(period, protocol);
            period.BallotsQuorum = GetBallotQuorum(period, protocol);
            period.Supermajority = 8000;

            period.YayBallots = 0;
            period.YayVotingPower = 0;
            period.NayBallots = 0;
            period.NayVotingPower = 0;
            period.PassBallots = 0;
            period.PassVotingPower = 0;
            #endregion

            Db.VotingSnapshots.AddRange(snapshots);
            Db.VotingPeriods.Add(period);
            Cache.Periods.Add(period);
            return period;
        }

        protected VotingPeriod StartWaitingPeriod(Block block, Protocol protocol, VotingPeriod current, PeriodKind kind)
        {
            var period = new VotingPeriod
            {
                Id = 0,
                Index = current.Index + 1,
                Epoch = current.Epoch,
                FirstLevel = block.Level + 1,
                LastLevel = block.Level + protocol.BlocksPerVoting,
                Kind = kind,
                Status = PeriodStatus.Active
            };

            #region snapshot
            var snapshots = Cache.Accounts.GetDelegates()
                .Where(x => BakerIsListed(x, block, protocol))
                .Select(x => new VotingSnapshot
                {
                    Id = 0,
                    Level = block.Level,
                    Period = period.Index,
                    BakerId = x.Id,
                    VotingPower = GetVotingPower(x, block, protocol),
                    Status = VoterStatus.None
                });

            period.TotalBakers = snapshots.Count();
            period.TotalVotingPower = snapshots.Sum(x => x.VotingPower);
            #endregion

            //#region temporary diagnostics
            //var listing = Proto.Node.GetAsync($"chains/main/blocks/{block.Level}/context/raw/json/votes/listings?depth=1").Result
            //    .EnumerateArray()
            //    .ToDictionary(x => x[0].RequiredString(), x => x[1].RequiredInt64());

            //if (snapshots.Count() != listing.Count)
            //    throw new Exception("Wrong voting snapshots");

            //foreach (var snapshot in snapshots)
            //    if (!listing.TryGetValue(Cache.Accounts.GetDelegate(snapshot.BakerId).Address, out var votingPower) || snapshot.VotingPower != votingPower)
            //        throw new Exception("Wrong voting snapshot");
            //#endregion

            Db.VotingSnapshots.AddRange(snapshots);
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
                var participation = 10000 * (prev.YayVotingPower!.Value + prev.NayVotingPower!.Value + prev.PassVotingPower!.Value) / prev.TotalVotingPower;
                return (int)((prev.ParticipationEma!.Value * 8000 + participation * 2000) / 10000);
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
                var participation = 10000 * (prev.YayVotingPower!.Value + prev.NayVotingPower!.Value + prev.PassVotingPower!.Value) / prev.TotalVotingPower;
                return (int)((prev.BallotsQuorum!.Value * 8000 + participation * 2000) / 10000);
            }

            return 8000;
        }

        protected virtual long GetVotingPower(Data.Models.Delegate baker, Block block, Protocol protocol)
        {
            return baker.StakingBalance - baker.StakingBalance % protocol.MinimalStake;
        }

        protected virtual bool BakerIsListed(Data.Models.Delegate baker, Block block, Protocol protocol)
        {
            return baker.Staked && baker.StakingBalance >= protocol.MinimalStake;
        }
    }
}
