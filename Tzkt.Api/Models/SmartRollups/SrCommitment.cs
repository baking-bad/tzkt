namespace Tzkt.Api.Models
{
    public class SrCommitment
    {
        /// <summary>
        /// Internal TzKT id.  
        /// **[sortable]**
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Smart rollup.
        /// </summary>
        public Alias Rollup { get; set; }

        /// <summary>
        /// Account that published the commitment first.
        /// </summary>
        public Alias Initiator { get; set; }

        /// <summary>
        /// Inbox level.  
        /// **[sortable]**
        /// </summary>
        public int InboxLevel { get; set; }

        /// <summary>
        /// State hash.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Commitment hash.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Number of ticks.
        /// </summary>
        public long Ticks { get; set; }

        /// <summary>
        /// Level of the block where the commitment was first published.  
        /// **[sortable]**
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the commitment was first published.
        /// </summary>
        public DateTime FirstTime { get; set; }

        /// <summary>
        /// Level of the block where the commitment was last updated.  
        /// **[sortable]**
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the commitment was last updated.
        /// </summary>
        public DateTime LastTime { get; set; }

        /// <summary>
        /// Number of stakers, published this commitment.  
        /// **[sortable]**
        /// </summary>
        public int Stakers { get; set; }

        /// <summary>
        /// Number of active (not refuted) stakers.  
        /// **[sortable]**
        /// </summary>
        public int ActiveStakers { get; set; }

        /// <summary>
        /// Number of successor commitments.  
        /// **[sortable]**
        /// </summary>
        public int Successors { get; set; }

        /// <summary>
        /// Commitment status (`pending`, `cemented`, `executed`, `refuted`, or `orphan`).
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Predecessor commitment.
        /// </summary>
        public SrCommitmentInfo Predecessor { get; set; }
    }
}
