using System.ComponentModel.DataAnnotations.Schema;

namespace Tezzycat.Models
{
    public class BakerStat
    {
        public int Id { get; set; }
        public int Cycle { get; set; }
        public int BakerId { get; set; }

        #region staking
        public long Balance { get; set; }
        public long StakingBalance { get; set; }
        #endregion

        #region baking
        public int Blocks { get; set; }
        public int BlocksMissed { get; set; }
        public int BlocksExtra { get; set; }

        public int Endorsements { get; set; }
        public int EndorsementsMissed { get; set; }
        #endregion

        #region rewards
        public long BlocksReward { get; set; }
        public long EndorsementsReward { get; set; }
        public long FeesReward { get; set; }

        public long AccusationReward { get; set; }
        public long AccusationLostDeposit { get; set; }
        public long AccusationLostReward { get; set; }
        public long AccusationLostFee { get; set; }

        public long RevelationReward { get; set; }
        public long RevelationLostReward { get; set; }
        public long RevelationLostFee { get; set; }
        #endregion

        #region relations
        [ForeignKey("BakerId")]
        public Contract Baker { get; set; }
        #endregion
    }
}
