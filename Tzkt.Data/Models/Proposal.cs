using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Data.Models
{
    public class Proposal
    {
        public int Id { get; set; }
        public string Hash { get; set; }

        #region relations
        public List<BallotOperation> Ballots { get; set; }
        public List<ProposalOperation> Proposals { get; set; }
        #endregion
    }
}
