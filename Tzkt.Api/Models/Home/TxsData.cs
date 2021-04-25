using System.Collections.Generic;

namespace Tzkt.Api.Models.Home
{
    public class TxsData
    {
        public List<ChartPoint> Chart { get; set; }
        public long TxsForMonth { get; set; }
        public double TxsDiff { get; set; }
        
        public long Volume { get; set; }
        public double VolumeDiff { get; set; }

        public long PaidFeesForMonth { get; set; }
        public double PaidDiff { get; set; }

        public long BurnedForMonth { get; set; }
        public double BurnedDiff { get; set; }
    }
}