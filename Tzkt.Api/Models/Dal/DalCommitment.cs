namespace Tzkt.Api.Models
{
    public class DalCommitment
    {
        /// <summary>
        /// Level at which the commitment has been published.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Slot index associated with the commitment.
        /// </summary>
        public int SlotIndex { get; set; }

        /// <summary>
        /// Hash of the commitment.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Information about the account who has published the commitment.
        /// </summary>
        public Alias Publisher { get; set; }
    }
}
