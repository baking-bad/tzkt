namespace Tzkt.Api.Models
{
    public class ProposalData
    {
        public string Hash { get; set; }
        public RawJson Metadata { get; set; }
        public long VotingPower { get; set; }
        public double VotingPowerPercentage { get; set; }

        #region deprecated
        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int Rolls => (int)(VotingPower / 6_000_000_000);

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public double RollsPercentage => VotingPowerPercentage;
        #endregion
    }
}