using System.Collections.Generic;

namespace Tzkt.Data.Models
{
    public class VotingEpoch
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int Progress { get; set; }

        #region relations
        public List<VotingPeriod> Periods { get; set; }
        #endregion
    }
}
