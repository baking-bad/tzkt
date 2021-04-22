using System.Collections.Generic;

namespace Tzkt.Api.Models.Home
{
    public class AccountsData
    {
        public long TotalAccounts { get; set; }
        public IEnumerable<ChartPoint> Chart { get; set; }
        public long FundedAccounts { get; set; }
        public long ActiveAccounts { get; set; }
        public long PublicAccounts { get; set; }
    }
}