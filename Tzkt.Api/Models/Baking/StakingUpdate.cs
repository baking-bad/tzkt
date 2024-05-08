using System.Numerics;

namespace Tzkt.Api.Models
{
    public class StakingUpdate
    {
        /// <summary>
        /// Internal TzKT ID.  
        /// **[sortable]**
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Level of the block where the staking update happened.  
        /// **[sortable]**
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Timestamp of the block where the staking update happened.  
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// For `unstake`, `restake`, `finalize` and `slash_unstaked` update types it's freezer cycle, othrewise it's cycle of the block.
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// Related baker.
        /// </summary>
        public Alias Baker { get; set; }

        /// <summary>
        /// Related staker.
        /// </summary>
        public Alias Staker { get; set; }

        /// <summary>
        /// Staking update type (`stake`, `unstake`, `restake`, `finalize`, `slash_staked`, `slash_unstaked`).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Amount (mutez).
        /// </summary>
        public long Amount { get; set; }

        /// <summary>
        /// Amount of staking pseudotokens minted or burnt.
        /// </summary>
        public BigInteger? Pseudotokens { get; set; }

        /// <summary>
        /// Protocol rounding error, appearing after slashing.
        /// </summary>
        public long? RoundingError { get; set; }

        /// <summary>
        /// Id of the operation, caused the staking update.
        /// If all `..OpId` fields are null, then the staking update was produced by the protocol migration.
        /// </summary>
        public long? AutostakingOpId { get; set; }

        /// <summary>
        /// Id of the operation, caused the staking update.
        /// If all `..OpId` fields are null, then the staking update was produced by the protocol migration.
        /// </summary>
        public long? StakingOpId { get; set; }

        /// <summary>
        /// Id of the operation, caused the staking update.
        /// If all `..OpId` fields are null, then the staking update was produced by the protocol migration.
        /// </summary>
        public long? DelegationOpId { get; set; }

        /// <summary>
        /// Id of the operation, caused the staking update.
        /// If all `..OpId` fields are null, then the staking update was produced by the protocol migration.
        /// </summary>
        public long? DoubleBakingOpId { get; set; }

        /// <summary>
        /// Id of the operation, caused the staking update.
        /// If all `..OpId` fields are null, then the staking update was produced by the protocol migration.
        /// </summary>
        public long? DoubleEndorsingOpId { get; set; }

        /// <summary>
        /// Id of the operation, caused the staking update.
        /// If all `..OpId` fields are null, then the staking update was produced by the protocol migration.
        /// </summary>
        public long? DoublePreendorsingOpId { get; set; }
    }
}
