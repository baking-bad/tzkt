﻿namespace Tzkt.Api.Models
{
    public class SrCommitmentInfo
    {
        /// <summary>
        /// Internal TzKT id.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Account that published the commitment first.
        /// </summary>
        public required Alias Initiator { get; set; }

        /// <summary>
        /// Inbox level
        /// </summary>
        public int InboxLevel { get; set; }

        /// <summary>
        /// State hash
        /// </summary>
        public required string State { get; set; }

        /// <summary>
        /// Commitment hash
        /// </summary>
        public required string Hash { get; set; }

        /// <summary>
        /// Number of ticks
        /// </summary>
        public long Ticks { get; set; }

        /// <summary>
        /// Level of the block where the commitment was first published.
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the commitment was first published.
        /// </summary>
        public DateTime FirstTime { get; set; }
    }
}
