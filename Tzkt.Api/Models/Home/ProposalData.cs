using Tzkt.Api.Services.Metadata;

namespace Tzkt.Api.Models
{
    public class ProposalData
    {
        public string Hash { get; set; }
        public ProposalMetadata Metadata { get; set; }
        public int Rolls { get; set; }
        public double RollsPercentage { get; set; }
    }
}