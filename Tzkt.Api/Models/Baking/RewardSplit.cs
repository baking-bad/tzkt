using System;
using System.Collections.Generic;

namespace Tzkt.Api.Models
{
    public class RewardSplit
    {
        public int Cycle { get; set; }
        public long StakingBalance { get; set; }
        public long DelegatedBalance { get; set; }
        public int NumDelegators { get; set; }

        public double ExpectedBlocks { get; set; }
        public double ExpectedEndorsements { get; set; }

        public int FutureBlocks { get; set; }
        public long FutureBlockRewards { get; set; }
        public long FutureBlockDeposits { get; set; }
        public int OwnBlocks { get; set; }
        public long OwnBlockRewards { get; set; }
        public int ExtraBlocks { get; set; }
        public long ExtraBlockRewards { get; set; }
        public int MissedOwnBlocks { get; set; }
        public long MissedOwnBlockRewards { get; set; }
        public int MissedExtraBlocks { get; set; }
        public long MissedExtraBlockRewards { get; set; }
        public int UncoveredOwnBlocks { get; set; }
        public long UncoveredOwnBlockRewards { get; set; }
        public int UncoveredExtraBlocks { get; set; }
        public long UncoveredExtraBlockRewards { get; set; }
        public long BlockDeposits { get; set; }

        public int FutureEndorsements { get; set; }
        public long FutureEndorsementRewards { get; set; }
        public long FutureEndorsementDeposits { get; set; }
        public int Endorsements { get; set; }
        public long EndorsementRewards { get; set; }
        public int MissedEndorsements { get; set; }
        public long MissedEndorsementRewards { get; set; }
        public int UncoveredEndorsements { get; set; }
        public long UncoveredEndorsementRewards { get; set; }
        public long EndorsementDeposits { get; set; }

        public long OwnBlockFees { get; set; }
        public long ExtraBlockFees { get; set; }
        public long MissedOwnBlockFees { get; set; }
        public long MissedExtraBlockFees { get; set; }
        public long UncoveredOwnBlockFees { get; set; }
        public long UncoveredExtraBlockFees { get; set; }

        public long AccusationRewards { get; set; }
        public long AccusationLostDeposits { get; set; }
        public long AccusationLostRewards { get; set; }
        public long AccusationLostFees { get; set; }

        public long RevelationRewards { get; set; }
        public long RevelationLostRewards { get; set; }
        public long RevelationLostFees { get; set; }

        public IEnumerable<SplitDelegator> Delegators { get; set; }
    }
}
