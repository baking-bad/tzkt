namespace Tzkt.Data.Models
{
    public class Cycle
    {
        public int Id { get; set; }
        public int Index { get; set; }

        #region shapshot
        public int Snapshot { get; set; }
        public int ActiveBakers { get; set; }
        public int ActiveDelegators { get; set; }
        public int TotalRolls { get; set; }
        public long TotalBalances { get; set; }
        #endregion

        #region stats
        #endregion
    }
}
