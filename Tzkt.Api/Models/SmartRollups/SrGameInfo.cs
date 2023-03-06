namespace Tzkt.Api.Models
{
    public class SrGameInfo
    {
        /// <summary>
        /// Internal TzKT id.  
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Initiator, who found a wrong commitment and started the refutation game.
        /// </summary>
        public Alias Initiator { get; set; }

        /// <summary>
        /// Initiator's verson of a valid commitment
        /// </summary>
        public SrCommitmentInfo InitiatorCommitment { get; set; }

        /// <summary>
        /// Opponent, who was acused in publishing a wrong commitment.
        /// </summary>
        public Alias Opponent { get; set; }

        /// <summary>
        /// Opponent's version of a valid commitment
        /// </summary>
        public SrCommitmentInfo OpponentCommitment { get; set; }

        /// <summary>
        /// In case the initiator won, this field will contain the reward amount (in mutez).
        /// </summary>
        public long? InitiatorReward { get; set; }

        /// <summary>
        /// In case the initiator lost (including a `draw`), this field will contain the loss amount (in mutez).
        /// </summary>
        public long? InitiatorLoss { get; set; }

        /// <summary>
        /// In case the opponent won, this field will contain the reward amount (in mutez).
        /// </summary>
        public long? OpponentReward { get; set; }

        /// <summary>
        /// In case the opponent lost (including a `draw`), this field will contain the loss amount (in mutez).
        /// </summary>
        public long? OpponentLoss { get; set; }
    }
}
