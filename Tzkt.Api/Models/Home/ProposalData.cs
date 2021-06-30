namespace Tzkt.Api.Models
{
    public class ProposalData
    {
        public string Hash { get; set; }
        public RawJson Metadata { get; set; }
        public int Rolls { get; set; }
        public double RollsPercentage { get; set; }
    }
}