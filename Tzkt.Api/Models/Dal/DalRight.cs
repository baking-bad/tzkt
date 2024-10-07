namespace Tzkt.Api.Models
{
    public class DalRight
    {
        /// <summary>
        /// Cycle for which the shards have been computed.
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// Level at which the shards can be attested.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Number of DAL shards the delegate has to attest.
        /// </summary>
        public int Shards { get; set; }

        /// <summary>
        /// Delegate to which right has been given.
        /// </summary>
        public Alias Delegate { get; set; }
    }
}

