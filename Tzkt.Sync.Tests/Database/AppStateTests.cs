using Microsoft.EntityFrameworkCore;
using Netezos.Rpc;
using Tzkt.Data;

namespace Tzkt.Sync.Tests.Database
{
    internal class AppStateTests
    {
        public static async Task RunAsync(TzktContext db, TezosRpc rpc)
        {
            var state = await db.AppState.SingleAsync();
            var block = await rpc.Blocks[state.Level].GetAsync();

            #region head
            if (state.Cycle != block.metadata.level_info.cycle)
                throw new Exception("Invalid AppState.Cycle");

            if (state.Level != block.metadata.level_info.level)
                throw new Exception("Invalid AppState.Level");
            
            if (state.Timestamp != block.header.timestamp)
                throw new Exception("Invalid AppState.Timestamp");

            if (state.Protocol != block.protocol)
                throw new Exception("Invalid AppState.Protocol");

            if (state.NextProtocol != block.metadata.next_protocol)
                throw new Exception("Invalid AppState.NextProtocol");

            if (state.Hash != block.hash)
                throw new Exception("Invalid AppState.Hash");

            if (state.VotingPeriod != block.metadata.voting_period_info.voting_period.index)
                throw new Exception("Invalid AppState.VotingPeriod");
            #endregion

            #region counters
            if (state.AccountCounter != await db.Accounts.CountAsync())
                throw new Exception("Invalid AppState.AccountCounter");

            var opsCount = state.ActivationOpsCount +
                state.BallotOpsCount +
                state.BlocksCount +
                state.DelegationOpsCount +
                state.DoubleBakingOpsCount +
                state.DoubleEndorsingOpsCount +
                state.DoublePreendorsingOpsCount +
                state.DrainDelegateOpsCount +
                state.EndorsementOpsCount +
                state.EndorsingRewardOpsCount +
                state.IncreasePaidStorageOpsCount +
                state.MigrationOpsCount +
                state.NonceRevelationOpsCount +
                state.OriginationOpsCount +
                state.PreendorsementOpsCount +
                state.ProposalOpsCount +
                state.RegisterConstantOpsCount +
                state.RevealOpsCount +
                state.RevelationPenaltyOpsCount +
                state.SetDepositsLimitOpsCount +
                state.TransactionOpsCount +
                state.TransferTicketOpsCount +
                state.TxRollupCommitOpsCount +
                state.TxRollupDispatchTicketsOpsCount +
                state.TxRollupFinalizeCommitmentOpsCount +
                state.TxRollupOriginationOpsCount +
                state.TxRollupRejectionOpsCount +
                state.TxRollupRemoveCommitmentOpsCount +
                state.TxRollupReturnBondOpsCount +
                state.TxRollupSubmitBatchOpsCount +
                state.UpdateConsensusKeyOpsCount +
                state.VdfRevelationOpsCount;

            if (state.OperationCounter != opsCount)
                throw new Exception("Invalid AppState.OperationCounter");

            var managerOpsCount = await db.DelegationOps.CountAsync(x => x.InitiatorId == null) +
                await db.OriginationOps.CountAsync(x => x.InitiatorId == null) +
                await db.TransactionOps.CountAsync(x => x.InitiatorId == null) +
                state.IncreasePaidStorageOpsCount +
                state.RegisterConstantOpsCount +
                state.RevealOpsCount +
                state.SetDepositsLimitOpsCount +
                state.TransferTicketOpsCount +
                state.TxRollupCommitOpsCount +
                state.TxRollupDispatchTicketsOpsCount +
                state.TxRollupFinalizeCommitmentOpsCount +
                state.TxRollupOriginationOpsCount +
                state.TxRollupRejectionOpsCount +
                state.TxRollupRemoveCommitmentOpsCount +
                state.TxRollupReturnBondOpsCount +
                state.TxRollupSubmitBatchOpsCount +
                state.UpdateConsensusKeyOpsCount;

            if (state.ManagerCounter != managerOpsCount)
                throw new Exception("Invalid AppState.ManagerCounter");

            if (state.BigMapCounter != await db.BigMaps.CountAsync())
                throw new Exception("Invalid AppState.BigMapCounter");

            if (state.BigMapKeyCounter != await db.BigMapKeys.CountAsync())
                throw new Exception("Invalid AppState.BigMapKeyCounter");

            if (state.BigMapUpdateCounter != await db.BigMapUpdates.CountAsync())
                throw new Exception("Invalid AppState.BigMapUpdateCounter");

            if (state.StorageCounter != await db.Storages.CountAsync())
                throw new Exception("Invalid AppState.StorageCounter");

            if (state.ScriptCounter != await db.Scripts.CountAsync())
                throw new Exception("Invalid AppState.ScriptCounter");

            if (state.EventCounter != await db.Events.CountAsync())
                throw new Exception("Invalid AppState.EventCounter");
            #endregion

            #region counts
            if (state.CommitmentsCount != await db.Commitments.CountAsync())
                throw new Exception("Invalid AppState.CommitmentsCount");

            if (state.AccountsCount != await db.Accounts.CountAsync())
                throw new Exception("Invalid AppState.AccountsCount");

            if (state.BlocksCount != await db.Blocks.CountAsync())
                throw new Exception("Invalid AppState.BlocksCount");

            if (state.ProtocolsCount != await db.Protocols.CountAsync())
                throw new Exception("Invalid AppState.ProtocolsCount");

            if (state.ActivationOpsCount != await db.ActivationOps.CountAsync())
                throw new Exception("Invalid AppState.ActivationOpsCount");

            if (state.BallotOpsCount != await db.BallotOps.CountAsync())
                throw new Exception("Invalid AppState.BallotOpsCount");

            if (state.DelegationOpsCount != await db.DelegationOps.CountAsync())
                throw new Exception("Invalid AppState.DelegationOpsCount");

            if (state.DoubleBakingOpsCount != await db.DoubleBakingOps.CountAsync())
                throw new Exception("Invalid AppState.DoubleBakingOpsCount");

            if (state.DoubleEndorsingOpsCount != await db.DoubleEndorsingOps.CountAsync())
                throw new Exception("Invalid AppState.DoubleEndorsingOpsCount");

            if (state.DoublePreendorsingOpsCount != await db.DoublePreendorsingOps.CountAsync())
                throw new Exception("Invalid AppState.DoublePreendorsingOpsCount");

            if (state.EndorsementOpsCount != await db.EndorsementOps.CountAsync())
                throw new Exception("Invalid AppState.EndorsementOpsCount");

            if (state.PreendorsementOpsCount != await db.PreendorsementOps.CountAsync())
                throw new Exception("Invalid AppState.PreendorsementOpsCount");

            if (state.NonceRevelationOpsCount != await db.NonceRevelationOps.CountAsync())
                throw new Exception("Invalid AppState.NonceRevelationOpsCount");

            if (state.VdfRevelationOpsCount != await db.VdfRevelationOps.CountAsync())
                throw new Exception("Invalid AppState.VdfRevelationOpsCount");

            if (state.OriginationOpsCount != await db.OriginationOps.CountAsync())
                throw new Exception("Invalid AppState.OriginationOpsCount");

            if (state.ProposalOpsCount != await db.ProposalOps.CountAsync())
                throw new Exception("Invalid AppState.ProposalOpsCount");

            if (state.RevealOpsCount != await db.RevealOps.CountAsync())
                throw new Exception("Invalid AppState.RevealOpsCount");

            if (state.TransactionOpsCount != await db.TransactionOps.CountAsync())
                throw new Exception("Invalid AppState.TransactionOpsCount");

            if (state.RegisterConstantOpsCount != await db.RegisterConstantOps.CountAsync())
                throw new Exception("Invalid AppState.RegisterConstantOpsCount");

            if (state.EndorsingRewardOpsCount != await db.EndorsingRewardOps.CountAsync())
                throw new Exception("Invalid AppState.EndorsingRewardOpsCount");

            if (state.SetDepositsLimitOpsCount != await db.SetDepositsLimitOps.CountAsync())
                throw new Exception("Invalid AppState.SetDepositsLimitOpsCount");

            if (state.TxRollupOriginationOpsCount != await db.TxRollupOriginationOps.CountAsync())
                throw new Exception("Invalid AppState.TxRollupOriginationOpsCount");

            if (state.TxRollupSubmitBatchOpsCount != await db.TxRollupSubmitBatchOps.CountAsync())
                throw new Exception("Invalid AppState.TxRollupSubmitBatchOpsCount");

            if (state.TxRollupCommitOpsCount != await db.TxRollupCommitOps.CountAsync())
                throw new Exception("Invalid AppState.TxRollupCommitOpsCount");

            if (state.TxRollupFinalizeCommitmentOpsCount != await db.TxRollupFinalizeCommitmentOps.CountAsync())
                throw new Exception("Invalid AppState.TxRollupFinalizeCommitmentOpsCount");

            if (state.TxRollupRemoveCommitmentOpsCount != await db.TxRollupRemoveCommitmentOps.CountAsync())
                throw new Exception("Invalid AppState.TxRollupRemoveCommitmentOpsCount");

            if (state.TxRollupReturnBondOpsCount != await db.TxRollupReturnBondOps.CountAsync())
                throw new Exception("Invalid AppState.TxRollupReturnBondOpsCount");

            if (state.TxRollupRejectionOpsCount != await db.TxRollupRejectionOps.CountAsync())
                throw new Exception("Invalid AppState.TxRollupRejectionOpsCount");

            if (state.TxRollupDispatchTicketsOpsCount != await db.TxRollupDispatchTicketsOps.CountAsync())
                throw new Exception("Invalid AppState.TxRollupDispatchTicketsOpsCount");

            if (state.TransferTicketOpsCount != await db.TransferTicketOps.CountAsync())
                throw new Exception("Invalid AppState.TransferTicketOpsCount");

            if (state.IncreasePaidStorageOpsCount != await db.IncreasePaidStorageOps.CountAsync())
                throw new Exception("Invalid AppState.IncreasePaidStorageOpsCount");

            if (state.UpdateConsensusKeyOpsCount != await db.UpdateConsensusKeyOps.CountAsync())
                throw new Exception("Invalid AppState.UpdateConsensusKeyOpsCount");

            if (state.DrainDelegateOpsCount != await db.DrainDelegateOps.CountAsync())
                throw new Exception("Invalid AppState.DrainDelegateOpsCount");

            if (state.MigrationOpsCount != await db.MigrationOps.CountAsync())
                throw new Exception("Invalid AppState.MigrationOpsCount");

            if (state.RevelationPenaltyOpsCount != await db.RevelationPenaltyOps.CountAsync())
                throw new Exception("Invalid AppState.RevelationPenaltyOpsCount");

            if (state.ProposalsCount != await db.Proposals.CountAsync())
                throw new Exception("Invalid AppState.ProposalsCount");

            if (state.CyclesCount != await db.Cycles.CountAsync())
                throw new Exception("Invalid AppState.CyclesCount");

            if (state.ConstantsCount != await db.RegisterConstantOps.CountAsync(x => x.Address != null))
                throw new Exception("Invalid AppState.ConstantsCount");

            if (state.TokensCount != await db.Tokens.CountAsync())
                throw new Exception("Invalid AppState.TokensCount");

            if (state.TokenBalancesCount != await db.TokenBalances.CountAsync())
                throw new Exception("Invalid AppState.TokenBalancesCount");

            if (state.TokenTransfersCount != await db.TokenTransfers.CountAsync())
                throw new Exception("Invalid AppState.TokenTransfersCount");

            if (state.EventsCount != await db.Events.CountAsync())
                throw new Exception("Invalid AppState.EventsCount");
            #endregion

            #region quotes
            var quotesCnt = await db.Quotes.CountAsync();
            if (!(state.QuoteLevel == -1 && quotesCnt == 0 || state.QuoteLevel >= 0 && state.QuoteLevel == quotesCnt - 1))
                throw new Exception("Invalid AppState.QuoteLevel");
            #endregion
        }
    }
}
