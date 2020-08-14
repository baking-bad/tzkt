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

        public int Level { get; set; }
        public DateTime Timestamp { get; set; }
        public string Protocol { get; set; }
        public string NextProtocol { get; set; }
        public string Hash { get; set; }

        public int AccountCounter { get; set; }
        public int OperationCounter { get; set; }
        public int ManagerCounter { get; set; }

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
        #endregion

        #region supply
        public long TotalBootstrapped { get; set; }
        public long TotalCommitments { get; set; }
        public long TotalActivated { get; set; }

        public long TotalCreated { get; set; }
        public long TotalBurned { get; set; }
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
                    Level = -1,
                    Timestamp = DateTime.MinValue,
                    Protocol = "",
                    NextProtocol = "",
                    Hash = "",
                    QuoteLevel = -1
                });
        }
    }
}
