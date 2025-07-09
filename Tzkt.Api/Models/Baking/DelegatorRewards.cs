using System.Numerics;
using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class DelegatorRewards
    {
        /// <summary>
        /// Cycle in which rewards were or will be earned.  
        /// **[sortable]**
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// Baker at the snapshot time.
        /// </summary>
        public required Alias Baker { get; set; }

        /// <summary>
        /// Amount delegated to the baker at the snapshot time (micro tez).
        /// This amount doesn't include staked amount.
        /// </summary>
        public long DelegatedBalance { get; set; }

        /// <summary>
        /// Amount of staked pseudotokens, representing staker's share within the baker's `externalStakedBalance` at the snapshot time.
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = true)]
        public BigInteger? StakedPseudotokens { get; set; }

        /// <summary>
        /// Estimated amount staked to the baker at the snapshot time (micro tez).
        /// It's computed on-the-fly as `externalStakedBalance * stakedPseudotokens / issuedPseudotokens`.
        /// </summary>
        public long? StakedBalance { get; set; }


        /// <summary>
        /// Rewards of the delegator's baker, from which the delegator can estimate his share, given his `delegatedBalance`.
        /// </summary>
        public required BakerRewards BakerRewards { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the end of the cycle
        /// </summary>
        public QuoteShort? Quote { get; set; }
        #endregion

        #region [DEPRECATED]
        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long BakingPower => BakerRewards.BakingPower;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long TotalBakingPower => BakerRewards.TotalBakingPower;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long BakerDelegatedBalance => BakerRewards.OwnDelegatedBalance;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long ExternalDelegatedBalance => BakerRewards.ExternalDelegatedBalance;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long BakerStakedBalance => BakerRewards.OwnStakedBalance;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long ExternalStakedBalance => BakerRewards.ExternalStakedBalance;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public double ExpectedBlocks => BakerRewards.ExpectedBlocks;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public double ExpectedEndorsements => BakerRewards.ExpectedAttestations;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long ExpectedDalShards => BakerRewards.ExpectedDalAttestations;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public int FutureBlocks => BakerRewards.FutureBlocks;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long FutureBlockRewards => BakerRewards.FutureBlockRewards;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public int Blocks => BakerRewards.Blocks;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long BlockRewardsDelegated => BakerRewards.BlockRewardsDelegated;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long BlockRewardsStakedOwn => BakerRewards.BlockRewardsStakedOwn;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long BlockRewardsStakedEdge => BakerRewards.BlockRewardsStakedEdge;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long BlockRewardsStakedShared => BakerRewards.BlockRewardsStakedShared;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public int MissedBlocks => BakerRewards.MissedBlocks;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long MissedBlockRewards => BakerRewards.MissedBlockRewards;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public int FutureEndorsements => BakerRewards.FutureAttestations;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long FutureEndorsementRewards => BakerRewards.FutureAttestationRewards;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public int Endorsements => BakerRewards.Attestations;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long EndorsementRewardsDelegated => BakerRewards.AttestationRewardsDelegated;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long EndorsementRewardsStakedOwn => BakerRewards.AttestationRewardsStakedOwn;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long EndorsementRewardsStakedEdge => BakerRewards.AttestationRewardsStakedEdge;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long EndorsementRewardsStakedShared => BakerRewards.AttestationRewardsStakedShared;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public int MissedEndorsements => BakerRewards.MissedAttestations;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long MissedEndorsementRewards => BakerRewards.MissedAttestationRewards;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long FutureDalAttestationRewards => BakerRewards.FutureDalAttestationRewards;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DalAttestationRewardsDelegated => BakerRewards.DalAttestationRewardsDelegated;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DalAttestationRewardsStakedOwn => BakerRewards.DalAttestationRewardsStakedOwn;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DalAttestationRewardsStakedEdge => BakerRewards.DalAttestationRewardsStakedEdge;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DalAttestationRewardsStakedShared => BakerRewards.DalAttestationRewardsStakedShared;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long MissedDalAttestationRewards => BakerRewards.MissedDalAttestationRewards;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long BlockFees => BakerRewards.BlockFees;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long MissedBlockFees => BakerRewards.MissedBlockFees;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DoubleBakingRewards => BakerRewards.DoubleBakingRewards;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DoubleBakingLostStaked => BakerRewards.DoubleBakingLostStaked;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DoubleBakingLostUnstaked => BakerRewards.DoubleBakingLostUnstaked;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DoubleBakingLostExternalStaked => BakerRewards.DoubleBakingLostExternalStaked;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DoubleBakingLostExternalUnstaked => BakerRewards.DoubleBakingLostExternalUnstaked;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DoubleEndorsingRewards => BakerRewards.DoubleEndorsingRewards;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DoubleEndorsingLostStaked => BakerRewards.DoubleEndorsingLostStaked;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DoubleEndorsingLostUnstaked => BakerRewards.DoubleEndorsingLostUnstaked;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DoubleEndorsingLostExternalStaked => BakerRewards.DoubleEndorsingLostExternalStaked;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DoubleEndorsingLostExternalUnstaked => BakerRewards.DoubleEndorsingLostExternalUnstaked;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DoublePreendorsingRewards => BakerRewards.DoublePreendorsingRewards;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DoublePreendorsingLostStaked => BakerRewards.DoublePreendorsingLostStaked;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DoublePreendorsingLostUnstaked => BakerRewards.DoublePreendorsingLostUnstaked;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DoublePreendorsingLostExternalStaked => BakerRewards.DoublePreendorsingLostExternalStaked;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long DoublePreendorsingLostExternalUnstaked => BakerRewards.DoublePreendorsingLostExternalUnstaked;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long VdfRevelationRewardsDelegated => BakerRewards.VdfRevelationRewardsDelegated;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long VdfRevelationRewardsStakedOwn => BakerRewards.VdfRevelationRewardsStakedOwn;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long VdfRevelationRewardsStakedEdge => BakerRewards.VdfRevelationRewardsStakedEdge;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long VdfRevelationRewardsStakedShared => BakerRewards.VdfRevelationRewardsStakedShared;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long NonceRevelationRewardsDelegated => BakerRewards.NonceRevelationRewardsDelegated;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long NonceRevelationRewardsStakedOwn => BakerRewards.NonceRevelationRewardsStakedOwn;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long NonceRevelationRewardsStakedEdge => BakerRewards.NonceRevelationRewardsStakedEdge;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long NonceRevelationRewardsStakedShared => BakerRewards.NonceRevelationRewardsStakedShared;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long NonceRevelationLosses => BakerRewards.NonceRevelationLosses;
        #endregion
    }
}
