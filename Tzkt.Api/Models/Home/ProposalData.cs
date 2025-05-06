namespace Tzkt.Api.Models
{
    public class ProposalData
    {
        public required string Hash { get; set; }
        public long VotingPower { get; set; }
        public double VotingPowerPercentage { get; set; }
        public RawJson? Extras { get; set; }
    }
}