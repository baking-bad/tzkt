namespace Tzkt.Api.Models.Home
{
    public class ContractsData
    {
        public int TotalContracts { get; set; }

        public int Calls { get; set; }
        public double CallsDiff { get; set; }
        
        public int Transfers { get; set; }
        public double TransfersDiff { get; set; }
        
        public long Burned { get; set; }
        public double BurnedDiff { get; set; }
    }
}