namespace Tzkt.Api.Models
{
    public class DailyData
    {
        public long Volume { get; set; }
        public double VolumeDiff { get; set; }
        public int Txs { get; set; }
        public double TxsDiff { get; set; }
        public int Calls { get; set; }
        public double CallsDiff { get; set; }
        public int Accounts { get; set; }
        public double AccountsDiff { get; set; }
    }
}