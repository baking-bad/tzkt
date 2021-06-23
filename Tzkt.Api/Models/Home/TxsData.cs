namespace Tzkt.Api.Models
{
    public class TxsData
    {
        public int Txs { get; set; }
        public double TxsDiff { get; set; }
        
        public long Volume { get; set; }
        public double VolumeDiff { get; set; }

        public long Fees { get; set; }
        public double FeesDiff { get; set; }

        public long Burned { get; set; }
        public double BurnedDiff { get; set; }
        
        public int Calls { get; set; }
        public double CallsDiff { get; set; }
    }
}