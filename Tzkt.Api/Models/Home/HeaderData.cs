namespace Tzkt.Api.Models.Home
{
    public class HeaderData
    {
        public long Volume { get; set; }
        public double VolumeDiff { get; set; }
        public long TxsCount { get; set; }
        public double TxsDiff { get; set; }
        public long ContractCalls { get; set; }
        public double CallsDiff { get; set; }
        public long NewAccounts { get; set; }
        public double NewAccountsDiff { get; set; }
    }
}