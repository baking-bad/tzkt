namespace Tzkt.Api.Models
{
    public class DalAttestationStatus
    {
        /// <summary>
        /// Level at which the related commitment has been published.
        /// </summary>
        public int PublishLevel { get; set; }

        /// <summary>
        /// Slot index of the related commitment.
        /// </summary>
        public int SlotIndex { get; set; }

        /// <summary>
        /// Hash of the related commitment.
        /// </summary>
        public string Commitment { get; set; }

        /// <summary>
        /// Information about the attester.
        /// </summary>
        public Alias Attester { get; set; }

        /// <summary>
        /// If the attester has attested the shards of the related commitment.
        /// </summary>
        public bool Attested { get; set; }
    }
}
