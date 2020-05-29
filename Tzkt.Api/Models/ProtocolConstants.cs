using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class ProtocolConstants
    {
        //TODO Think about it
        /// <summary>
        /// A number of cycles in which the bakers security deposit and rewards are frozen. Currently it is 5 cycles which is approximately 15 days
        /// </summary>
        public int PreservedCycles { get; set; }

        /// <summary>
        /// A number of blocks the cycle contains
        /// </summary>
        public int BlocksPerCycle { get; set; }
        
        /// <summary>
        /// A number of blocks that indicates how often seed nonce hash is included in a block. Seed nonce hash presents in only one out of `blocksPerCommitment`
        /// </summary>
        public int BlocksPerCommitment { get; set; }
        
        /// <summary>
        /// A number of blocks that indicates how often a snapshot (snapshots are records of the state of rolls distributions) is taken
        /// </summary>
        public int BlocksPerSnapshot { get; set; }
        
        /// <summary>
        /// A number of block that indicates how long a voting period takes
        /// </summary>
        public int BlocksPerVoting { get; set; }

        /// <summary>
        /// Minimum amount of seconds between blocks
        /// </summary>
        public int TimeBetweenBlocks { get; set; }

        /// <summary>
        /// Number of bakers that assigned to endorse a block
        /// </summary>
        public int EndorsersPerBlock { get; set; }
        
        /// <summary>
        /// Maximum amount of gas that one operation can consume
        /// </summary>
        public int HardOperationGasLimit { get; set; }
        
        /// <summary>
        /// Maximum amount of storage that one operation can consume
        /// </summary>
        public int HardOperationStorageLimit { get; set; }
        
        /// <summary>
        /// Maximum amount of total gas usage of a single block
        /// </summary>
        public int HardBlockGasLimit { get; set; }

        /// <summary>
        /// Required number of tokens to get 1 roll (micro tez)
        /// </summary>
        public long TokensPerRoll { get; set; }
        
        /// <summary>
        /// Reward for seed nonce revelation (micro tez)
        /// </summary>
        public long RevelationReward { get; set; }

        /// <summary>
        /// Security deposit for baking (producing) a block (micro tez)
        /// </summary>
        public long BlockDeposit { get; set; }
        
        //TODO Think about it
        /// <summary>
        /// Reward for baking (producing) a block (micro tez)
        /// </summary>
        public List<long> BlockReward { get; set; }

        /// <summary>
        /// Security deposit for sending an endorsement operation (micro tez)
        /// </summary>
        public long EndorsementDeposit { get; set; }
        
        /// <summary>
        /// Reward for sending an endorsement operation (micro tez)
        /// </summary>
        public List<long> EndorsementReward { get; set; }

        /// <summary>
        /// Initial storage size of an originated (created) account (bytes)
        /// </summary>
        public int OriginationSize { get; set; }
        
        /// <summary>
        /// Cost of one storage byte in the blockchain (micro tez)
        /// </summary>
        public int ByteCost { get; set; }
    }
}
