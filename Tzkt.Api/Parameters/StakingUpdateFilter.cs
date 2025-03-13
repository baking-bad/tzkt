using NSwag.Annotations;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class StakingUpdateFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? id { get; set; }

        /// <summary>
        /// Filter by level of the block, where the staking update happened.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? level { get; set; }

        /// <summary>
        /// Filter by timestamp of the block, where the staking update happened.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter? timestamp { get; set; }

        /// <summary>
        /// Filter by freezer or block cycle (depending on the update type).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? cycle { get; set; }

        /// <summary>
        /// Filter by related baker.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter? baker { get; set; }

        /// <summary>
        /// Filter by related staker.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter? staker { get; set; }

        /// <summary>
        /// Filter by staking update type.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public StakingUpdateTypeParameter? type { get; set; }

        /// <summary>
        /// Filter by amount.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter? amount { get; set; }

        /// <summary>
        /// Filter by amount of staking pseudotokens minted or burnt.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public BigIntegerNullableParameter? pseudotokens { get; set; }

        /// <summary>
        /// Filter by protocol rounding error.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64NullParameter? roundingError { get; set; }

        /// <summary>
        /// Filter by the ID of the related operation.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64NullParameter? autostakingOpId { get; set; }

        /// <summary>
        /// Filter by the ID of the related operation.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64NullParameter? stakingOpId { get; set; }

        /// <summary>
        /// Filter by the ID of the related operation.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64NullParameter? delegationOpId { get; set; }

        /// <summary>
        /// Filter by the ID of the related operation.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64NullParameter? doubleBakingOpId { get; set; }

        /// <summary>
        /// Filter by the ID of the related operation.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64NullParameter? doubleEndorsingOpId { get; set; }

        /// <summary>
        /// Filter by the ID of the related operation.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64NullParameter? doublePreendorsingOpId { get; set; }

        [OpenApiIgnore]
        public bool Empty =>
            id == null &&
            level == null &&
            timestamp == null &&
            cycle == null &&
            baker == null &&
            staker == null &&
            type == null &&
            amount == null &&
            pseudotokens == null &&
            roundingError == null &&
            autostakingOpId == null &&
            stakingOpId == null &&
            delegationOpId == null &&
            doubleBakingOpId == null &&
            doubleEndorsingOpId == null &&
            doublePreendorsingOpId == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("level", level), ("timestamp", timestamp), ("cycle", cycle), ("baker", baker),
                ("staker", staker), ("type", type), ("amount", amount), ("pseudotokens", pseudotokens),
                ("roundingError", roundingError), ("autostakingOpId", autostakingOpId), ("stakingOpId", stakingOpId),
                ("delegationOpId", delegationOpId), ("doubleBakingOpId", doubleBakingOpId),
                ("doubleEndorsingOpId", doubleEndorsingOpId), ("doublePreendorsingOpId", doublePreendorsingOpId));
        }
    }
}
