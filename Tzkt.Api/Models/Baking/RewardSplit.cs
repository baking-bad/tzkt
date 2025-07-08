using System.Numerics;
using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class RewardSplit : BakerRewards
    {
        /// <summary>
        /// List of delegators, forming baker's baking power.
        /// This list includes "unstakers" (delegators who left the baker, but still had locked unstaked balance delegated to the baker),
        /// therefore its length shouldn't necessarily match the `delegatorsCount` value.
        /// </summary>
        public required IEnumerable<SplitDelegator> Delegators { get; set; }
        /// <summary>
        /// List of stakers, forming baker's baking power.
        /// </summary>
        public required IEnumerable<SplitStaker> Stakers { get; set; }
        /// <summary>
        /// List of actual stakers, receiving staking rewards regardless of contribution to the baker's baking power.
        /// </summary>
        public required IEnumerable<SplitActualStaker> ActualStakers { get; set; }
    }

    public class SplitDelegator
    {
        /// <summary>
        /// Address of the delegator
        /// </summary>
        public required string Address { get; set; }

        /// <summary>
        /// Amount delegated to the baker at the snapshot time (micro tez).
        /// This amount doesn't include staked amount.
        /// </summary>
        public long DelegatedBalance { get; set; }

        /// <summary>
        /// Indicates whether the delegator is emptied (at the moment, not at the snapshot time).
        /// Emptied accounts (users with zero balance) should be re-allocated, so if you make payment to the emptied account you will pay allocation fee.
        /// </summary>
        public bool Emptied { get; set; }
    }

    public class SplitStaker
    {
        /// <summary>
        /// Address of the staker
        /// </summary>
        public required string Address { get; set; }

        /// <summary>
        /// Amount of staked pseudotokens at the snapshot time, representing staker's share within the baker's `externalStakedBalance`.
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = true)]
        public BigInteger StakedPseudotokens { get; set; }

        /// <summary>
        /// Estimated amount staked to the baker at the snapshot time (micro tez).
        /// It's computed on-the-fly as `externalStakedBalance * stakedPseudotokens / issuedPseudotokens`.
        /// </summary>
        public long StakedBalance { get; set; }
    }

    public class SplitActualStaker
    {
        /// <summary>
        /// Address of the delegator
        /// </summary>
        public required string Address { get; set; }

        /// <summary>
        /// Staked balance at the beginning of the cycle (micro tez).
        /// </summary>
        public long InitialStake { get; set; }

        /// <summary>
        /// Staked balance at the end of the cycle (micro tez).
        /// </summary>
        public long FinalStake { get; set; }

        /// <summary>
        /// Staking rewards (or losses if negative) earned during the cycle (micro tez).
        /// </summary>
        public long Rewards { get; set; }
    }

    public class SplitMember
    {
        /// <summary>
        /// Address of the delegator
        /// </summary>
        public required string Address { get; set; }

        /// <summary>
        /// Amount delegated to the baker at the snapshot time (micro tez).
        /// This amount doesn't include staked amount.
        /// </summary>
        public long DelegatedBalance { get; set; }

        /// <summary>
        /// Amount of staked pseudotokens at the snapshot time, representing staker's share within the baker's `externalStakedBalance`.
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = true)]
        public BigInteger StakedPseudotokens { get; set; }

        /// <summary>
        /// Estimated amount staked to the baker at the snapshot time (micro tez).
        /// It's computed on-the-fly as `externalStakedBalance * stakedPseudotokens / issuedPseudotokens`.
        /// </summary>
        public long StakedBalance { get; set; }

        /// <summary>
        /// Indicates whether the delegator is emptied (at the moment, not at the snapshot time).
        /// Emptied accounts (users with zero balance) should be re-allocated, so if you make payment to the emptied account you will pay allocation fee.
        /// </summary>
        public bool Emptied { get; set; }
    }
}
