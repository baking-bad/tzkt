namespace Tzkt.Api.Models
{
    public class BakingInterval
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalBlocks { get; set; }
        public int MissedBlocks { get; set; }
        public int FutureBlocks { get; set; }
        public int TotalAttestations { get; set; }
        public int MissedAttestations { get; set; }
        public int FutureAttestations { get; set; }
        public int? FirstLevel { get; set; }
        public int? LastLevel { get; set; }
    }
}
