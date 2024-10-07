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
        
        /// <summary>
        /// Total number of Shards attested by the delegates for this commitment
        /// </summary>
        public int ShardsAttested { get; set; }
        
        /// <summary>
        /// Attestation successful for the commitment
        /// </summary>
        public bool Attested { get; set; }
    }
}
