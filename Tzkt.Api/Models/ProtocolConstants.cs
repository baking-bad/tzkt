using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class ProtocolConstants
    {
        public int PreservedCycles { get; set; }

        public int BlocksPerCycle { get; set; }
        public int BlocksPerCommitment { get; set; }
        public int BlocksPerSnapshot { get; set; }
        public int BlocksPerVoting { get; set; }

        public int TimeBetweenBlocks { get; set; }

        public int EndorsersPerBlock { get; set; }
        public int HardOperationGasLimit { get; set; }
        public int HardOperationStorageLimit { get; set; }
        public int HardBlockGasLimit { get; set; }

        public long TokensPerRoll { get; set; }
        public long RevelationReward { get; set; }

        public long BlockDeposit { get; set; }
        public List<long> BlockReward { get; set; }

        public long EndorsementDeposit { get; set; }
        public List<long> EndorsementReward { get; set; }

        public int OriginationSize { get; set; }
        public int ByteCost { get; set; }
    }
}
