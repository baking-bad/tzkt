using System;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    //TODO: add chain id
    public class AppState
    {
        public int Id { get; set; }
        public int KnownHead { get; set; }
        public DateTime LastSync { get; set; }

        public int Cycle { get; set; }
        public int Level { get; set; }
        public DateTime Timestamp { get; set; }
        public string Protocol { get; set; }
        public string NextProtocol { get; set; }
        public string Hash { get; set; }

        public int VotingEpoch { get; set; }
        public int VotingPeriod { get; set; }

        public int AccountCounter { get; set; }
        public int OperationCounter { get; set; }
        public int ManagerCounter { get; set; }
        public int BigMapCounter { get; set; }
        public int BigMapKeyCounter { get; set; }
        public int BigMapUpdateCounter { get; set; }
        public int StorageCounter { get; set; }
        public int ScriptCounter { get; set; }

        #region entities count
        public int CommitmentsCount { get; set; }
        public int AccountsCount { get; set; }

        public int BlocksCount { get; set; }
        public int ProtocolsCount { get; set; }

        public int ActivationOpsCount { get; set; }
        public int BallotOpsCount { get; set; }
        public int DelegationOpsCount { get; set; }
        public int DoubleBakingOpsCount { get; set; }
        public int DoubleEndorsingOpsCount { get; set; }
        public int EndorsementOpsCount { get; set; }
        public int NonceRevelationOpsCount { get; set; }
        public int OriginationOpsCount { get; set; }
        public int ProposalOpsCount { get; set; }
        public int RevealOpsCount { get; set; }
        public int TransactionOpsCount { get; set; }

        public int MigrationOpsCount { get; set; }
        public int RevelationPenaltyOpsCount { get; set; }

        public int ProposalsCount { get; set; }

        public int CyclesCount { get; set; }
        #endregion

        #region quotes
        public int QuoteLevel { get; set; }
        public double QuoteBtc { get; set; }
        public double QuoteEur { get; set; }
        public double QuoteUsd { get; set; }
        public double QuoteCny { get; set; }
        public double QuoteJpy { get; set; }
        public double QuoteKrw { get; set; }
        public double QuoteEth { get; set; }
        #endregion
    }

    public static class AppStateModel
    {
        public static void BuildAppStateModel(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppState>().HasData(
                new AppState
                {
                    Id = -1,
                    Cycle = -1,
                    Level = -1,
                    Timestamp = DateTime.MinValue,
                    Protocol = "",
                    NextProtocol = "",
                    Hash = "",
                    VotingEpoch = -1,
                    VotingPeriod = -1,
                    QuoteLevel = -1
                });
        }
    }
}
