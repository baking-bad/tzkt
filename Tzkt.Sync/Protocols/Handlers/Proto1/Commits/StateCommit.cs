using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class StateCommit : ProtocolCommit
    {
        public StateCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual Task Apply(Block block, JsonElement rawBlock)
        {
            var nextProtocol = rawBlock.Required("metadata").RequiredString("next_protocol");
            var appState = Cache.AppState.Get();

            #region entities
            var state = appState;
            #endregion

            state.Cycle = block.Cycle;
            state.Level = block.Level;
            state.Timestamp = block.Timestamp;
            state.Protocol = block.Protocol.Hash;
            state.NextProtocol = nextProtocol;
            state.Hash = block.Hash;

            state.BlocksCount++;
            if (block.Events.HasFlag(BlockEvents.ProtocolBegin)) state.ProtocolsCount++;
            if (block.Events.HasFlag(BlockEvents.CycleBegin)) state.CyclesCount++;

            if (block.Activations != null)
                state.ActivationOpsCount += block.Activations.Count;

            //if (block.Ballots != null)
            //    state.BallotOpsCount += block.Ballots.Count;

            //if (block.Proposals != null)
            //    state.ProposalOpsCount += block.Proposals.Count;

            if (block.Delegations != null)
                state.DelegationOpsCount += block.Delegations.Count;

            if (block.DoubleBakings != null)
                state.DoubleBakingOpsCount += block.DoubleBakings.Count;

            if (block.DoubleEndorsings != null)
                state.DoubleEndorsingOpsCount += block.DoubleEndorsings.Count;

            if (block.DoublePreendorsings != null)
                state.DoublePreendorsingOpsCount += block.DoublePreendorsings.Count;

            if (block.Endorsements != null)
                state.EndorsementOpsCount += block.Endorsements.Count;

            if (block.Preendorsements != null)
                state.PreendorsementOpsCount += block.Preendorsements.Count;

            if (block.Revelations != null)
                state.NonceRevelationOpsCount += block.Revelations.Count;

            if (block.Originations != null)
                state.OriginationOpsCount += block.Originations.Count;

            if (block.Reveals != null)
                state.RevealOpsCount += block.Reveals.Count;

            if (block.RegisterConstants != null)
                state.RegisterConstantOpsCount += block.RegisterConstants.Count;

            if (block.Transactions != null)
                state.TransactionOpsCount += block.Transactions.Count;

            if (block.RevelationPenalties != null)
                state.RevelationPenaltyOpsCount += block.RevelationPenalties.Count;

            //if (block.Proposals != null)
            //    state.ProposalsCount += Db.ChangeTracker.Entries().Count(
            //        x => x.Entity is Proposal && x.State == Microsoft.EntityFrameworkCore.EntityState.Added);

            return Task.CompletedTask;
        }

        public virtual async Task Revert(Block block)
        {
            block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            var nextProtocol = block.Protocol.Hash;
            var appState = Cache.AppState.Get();

            #region entities
            var state = appState;
            var prevBlock = await Cache.Blocks.PreviousAsync();
            prevBlock.Protocol ??= await Cache.Protocols.GetAsync(prevBlock.ProtoCode);
            #endregion

            state.Cycle = prevBlock.Cycle;
            state.Level = prevBlock.Level;
            state.Timestamp = prevBlock.Timestamp;
            state.Protocol = prevBlock.Protocol.Hash;
            state.NextProtocol = nextProtocol;
            state.Hash = prevBlock.Hash;

            state.BlocksCount--;
            if (block.Events.HasFlag(BlockEvents.ProtocolBegin)) state.ProtocolsCount--;
            if (block.Events.HasFlag(BlockEvents.CycleBegin)) state.CyclesCount--;

            if (block.Activations != null)
                state.ActivationOpsCount -= block.Activations.Count;

            //if (block.Ballots != null)
            //    state.BallotOpsCount -= block.Ballots.Count;

            //if (block.Proposals != null)
            //    state.ProposalOpsCount -= block.Proposals.Count;

            if (block.Delegations != null)
                state.DelegationOpsCount -= block.Delegations.Count;

            if (block.DoubleBakings != null)
                state.DoubleBakingOpsCount -= block.DoubleBakings.Count;

            if (block.DoubleEndorsings != null)
                state.DoubleEndorsingOpsCount -= block.DoubleEndorsings.Count;

            if (block.DoublePreendorsings != null)
                state.DoublePreendorsingOpsCount -= block.DoublePreendorsings.Count;

            if (block.Endorsements != null)
                state.EndorsementOpsCount -= block.Endorsements.Count;

            if (block.Preendorsements != null)
                state.PreendorsementOpsCount -= block.Preendorsements.Count;

            if (block.Revelations != null)
                state.NonceRevelationOpsCount -= block.Revelations.Count;

            if (block.Originations != null)
                state.OriginationOpsCount -= block.Originations.Count;

            if (block.Reveals != null)
                state.RevealOpsCount -= block.Reveals.Count;

            if (block.RegisterConstants != null)
                state.RegisterConstantOpsCount -= block.RegisterConstants.Count;

            if (block.Transactions != null)
                state.TransactionOpsCount -= block.Transactions.Count;

            if (block.RevelationPenalties != null)
                state.RevelationPenaltyOpsCount -= block.RevelationPenalties.Count;

            //if (block.Proposals != null)
            //    state.ProposalsCount -= Db.ChangeTracker.Entries().Count(
            //        x => x.Entity is Proposal && x.State == Microsoft.EntityFrameworkCore.EntityState.Deleted);

            Cache.Blocks.Remove(block);
        }
    }
}
