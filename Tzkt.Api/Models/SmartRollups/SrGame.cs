namespace Tzkt.Api.Models
{
    public class SrGame
    {
        /// <summary>
        /// Internal TzKT id.  
        /// **[sortable]**
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Smart rollup, in which's scope the refutation game was started.
        /// </summary>
        public required Alias Rollup { get; set; }

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
        /// The most recent move (`sr_refute` operation).
        /// </summary>
        public required SrGameMove LastMove { get; set; }

        /// <summary>
        /// Level of the block where the refutation game was started.  
        /// **[sortable]**
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the refutation game was started.
        /// </summary>
        public DateTime FirstTime { get; set; }

        /// <summary>
        /// Level of the block where the refutation game was last updated.  
        /// **[sortable]**
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the refutation game was last updated.
        /// </summary>
        public DateTime LastTime { get; set; }

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
