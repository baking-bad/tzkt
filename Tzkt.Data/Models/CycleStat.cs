namespace Tzkt.Data.Models
{
    public class CycleStat
    {
        public int Id { get; set; }
        public int Cycle { get; set; }

        #region shapshot
        public int Snapshot { get; set; }
        public int ActiveBakers { get; set; }
        public int ActiveDelegators { get; set; }
        public int TotalBalances { get; set; }
        public int TotalRolls { get; set; }
        #endregion

        public int Transactions { get; set; }
        public int TransactionsVolume { get; set; }

        public int CreatedContracts { get; set; }
    }
}
