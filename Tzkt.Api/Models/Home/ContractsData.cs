using System.Collections.Generic;

namespace Tzkt.Api.Models.Home
{
    public class ContractsData
    {
        public IEnumerable<ChartPoint> Chart { get; set; }
        public long TotalContracts { get; set; }
        public long NewCalls { get; set; }
        public double CallsDiff { get; set; }
        
        public long Transfers { get; set; }
        public double TransfersDiff { get; set; }
        
        public long Burned { get; set; }
        public double BurnedDiff { get; set; }
    }
}