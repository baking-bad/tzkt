﻿namespace Tzkt.Api.Models
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
        public required Alias Initiator { get; set; }

        /// <summary>
        /// Initiator's version of a valid commitment
        /// </summary>
        public required SrCommitmentInfo InitiatorCommitment { get; set; }

        /// <summary>
        /// Opponent, who was accused in publishing a wrong commitment.
        /// </summary>
        public required Alias Opponent { get; set; }

        /// <summary>
        /// Opponent's version of a valid commitment
        /// </summary>
        public required SrCommitmentInfo OpponentCommitment { get; set; }

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
