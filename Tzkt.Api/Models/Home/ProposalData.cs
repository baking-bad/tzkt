namespace Tzkt.Api.Models
{
    public class ProposalData
    {
        public string Hash { get; set; }
        public RawJson Metadata { get; set; }
        public long VotingPower { get; set; }
        public double VotingPowerPercentage { get; set; }

        #region deprecated
        public int Rolls => (int)(VotingPower / 6_000_000_000);
        public double RollsPercentage => VotingPowerPercentage;
        #endregion
    }
}