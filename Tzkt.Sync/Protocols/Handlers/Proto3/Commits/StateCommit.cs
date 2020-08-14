using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class StateCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public AppState AppState { get; private set; }
        public string NextProtocol { get; private set; }

        StateCommit(ProtocolHandler protocol) : base(protocol) { }

        public Task Init(Block block, RawBlock rawBlock)
        {
            Block = block;
            NextProtocol = rawBlock.Metadata.NextProtocol;
            AppState = Cache.AppState.Get();
            return Task.CompletedTask;
        }

        public async Task Init(Block block)
        {
            Block = block;
            Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            NextProtocol = block.Protocol.Hash;
            AppState = Cache.AppState.Get();
        }

        public override Task Apply()
        {
            #region entities
            var state = AppState;

            Db.TryAttach(state);
            #endregion

            state.Level = Block.Level;
            state.Timestamp = Block.Timestamp;
            state.Protocol = Block.Protocol.Hash;
            state.NextProtocol = NextProtocol;
            state.Hash = Block.Hash;

            state.BlocksCount++;
            if (Block.Events.HasFlag(BlockEvents.ProtocolBegin)) state.ProtocolsCount++;
            if (Block.Events.HasFlag(BlockEvents.CycleBegin)) state.CyclesCount++;

            state.TotalCreated += Block.Reward;

            if (Block.Activations != null)
            {
                state.ActivationOpsCount += Block.Activations.Count;
                state.TotalActivated += Block.Activations.Sum(x => x.Balance);
            }

            if (Block.Ballots != null)
            {
                state.BallotOpsCount += Block.Ballots.Count;
            }

            if (Block.Proposals != null)
            {
                state.ProposalOpsCount += Block.Proposals.Count;
            }

            if (Block.Delegations != null)
            {
                state.DelegationOpsCount += Block.Delegations.Count;
            }

            if (Block.DoubleBakings != null)
            {
                state.DoubleBakingOpsCount += Block.DoubleBakings.Count;
                state.TotalBurned += Block.DoubleBakings.Sum(x => x.OffenderLostDeposit + x.OffenderLostReward + x.OffenderLostFee - x.AccuserReward);
            }

            if (Block.Endorsements != null)
            {
                state.EndorsementOpsCount += Block.Endorsements.Count;
                state.TotalCreated += Block.Endorsements.Sum(x => x.Reward);
            }

            if (Block.Revelations != null)
            {
                state.NonceRevelationOpsCount += Block.Revelations.Count;
                state.TotalCreated += Block.Revelations.Sum(x => 125_000); //TODO: avoid constants
            }

            if (Block.Originations != null)
            {
                state.OriginationOpsCount += Block.Originations.Count;
                state.TotalBurned += Block.Originations.Sum(x => x.StorageFee ?? 0 + x.AllocationFee ?? 0);
            }

            if (Block.Reveals != null)
            {
                state.RevealOpsCount += Block.Reveals.Count;
            }

            if (Block.Transactions != null)
            {
                state.TransactionOpsCount += Block.Transactions.Count;
                state.TotalBurned += Block.Transactions.Sum(x => x.StorageFee ?? 0 + x.AllocationFee ?? 0);
            }

            if (Block.RevelationPenalties != null)
            {
                state.RevelationPenaltyOpsCount += Block.RevelationPenalties.Count;
                state.TotalBurned += Block.RevelationPenalties.Sum(x => x.LostReward + x.LostFees);
            }

            if (Block.Proposals != null)
                state.ProposalsCount += Db.ChangeTracker.Entries().Count(
                    x => x.Entity is Proposal && x.State == Microsoft.EntityFrameworkCore.EntityState.Added);

            return Task.CompletedTask;
        }

        public override async Task Revert()
        {
            #region entities
            var state = AppState;
            var prevBlock = await Cache.Blocks.PreviousAsync();
            if (prevBlock != null) prevBlock.Protocol ??= await Cache.Protocols.GetAsync(prevBlock.ProtoCode);

            Db.TryAttach(state);
            #endregion

            state.Level = prevBlock?.Level ?? -1;
            state.Timestamp = prevBlock?.Timestamp ?? DateTime.MinValue;
            state.Protocol = prevBlock?.Protocol.Hash ?? "";
            state.NextProtocol = prevBlock == null ? "" : NextProtocol;
            state.Hash = prevBlock?.Hash ?? "";

            state.BlocksCount--;
            if (Block.Events.HasFlag(BlockEvents.ProtocolBegin)) state.ProtocolsCount--;
            if (Block.Events.HasFlag(BlockEvents.CycleBegin)) state.CyclesCount--;

            state.TotalCreated -= Block.Reward;

            if (Block.Activations != null)
            {
                state.ActivationOpsCount -= Block.Activations.Count;
                state.TotalActivated -= Block.Activations.Sum(x => x.Balance);
            }

            if (Block.Ballots != null)
            {
                state.BallotOpsCount -= Block.Ballots.Count;
            }

            if (Block.Proposals != null)
            {
                state.ProposalOpsCount -= Block.Proposals.Count;
            }

            if (Block.Delegations != null)
            {
                state.DelegationOpsCount -= Block.Delegations.Count;
            }

            if (Block.DoubleBakings != null)
            {
                state.DoubleBakingOpsCount -= Block.DoubleBakings.Count;
                state.TotalBurned -= Block.DoubleBakings.Sum(x => x.OffenderLostDeposit + x.OffenderLostReward + x.OffenderLostFee - x.AccuserReward);
            }

            if (Block.Endorsements != null)
            {
                state.EndorsementOpsCount -= Block.Endorsements.Count;
                state.TotalCreated -= Block.Endorsements.Sum(x => x.Reward);
            }

            if (Block.Revelations != null)
            {
                state.NonceRevelationOpsCount -= Block.Revelations.Count;
                state.TotalCreated -= Block.Revelations.Sum(x => 125_000); //TODO: avoid constants
            }

            if (Block.Originations != null)
            {
                state.OriginationOpsCount -= Block.Originations.Count;
                state.TotalBurned -= Block.Originations.Sum(x => x.StorageFee ?? 0 + x.AllocationFee ?? 0);
            }

            if (Block.Reveals != null)
            {
                state.RevealOpsCount -= Block.Reveals.Count;
            }

            if (Block.Transactions != null)
            {
                state.TransactionOpsCount -= Block.Transactions.Count;
                state.TotalBurned -= Block.Transactions.Sum(x => x.StorageFee ?? 0 + x.AllocationFee ?? 0);
            }

            if (Block.RevelationPenalties != null)
            {
                state.RevelationPenaltyOpsCount -= Block.RevelationPenalties.Count;
                state.TotalBurned -= Block.RevelationPenalties.Sum(x => x.LostReward + x.LostFees);
            }

            if (Block.Proposals != null)
                state.ProposalsCount -= Db.ChangeTracker.Entries().Count(
                    x => x.Entity is Proposal && x.State == Microsoft.EntityFrameworkCore.EntityState.Deleted);

            Cache.Blocks.Remove(Block);
        }

        #region static
        public static async Task<StateCommit> Apply(ProtocolHandler proto, Block block, RawBlock rawBlock)
        {
            var commit = new StateCommit(proto);
            await commit.Init(block, rawBlock);
            await commit.Apply();

            return commit;
        }

        public static async Task<StateCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new StateCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
