using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Services.Cache
{
    public class RawState
    {
        public int KnownHead { get; set; }

        public DateTime LastSync { get; set; }

        public int Level { get; set; }

        public string Hash { get; set; }

        public string Protocol { get; set; }

        public DateTime Timestamp { get; set; }

        public int OperationCounter { get; set; }

        #region entities count
        public int AccountsCount { get; set; }

        public int BlocksCount { get; set; }
        public int ProtocolsCount { get; set; }
        public int ProposalsCount { get; set; }
        public int CyclesCount { get; set; }

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
        #endregion
    }
}
