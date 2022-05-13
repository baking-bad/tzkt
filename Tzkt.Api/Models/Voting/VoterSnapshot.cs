using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class VoterSnapshot
    {
        /// <summary>
        /// Voter identity
        /// </summary>
        public Alias Delegate { get; set; }

        /// <summary>
        /// Baker's voting power
        /// </summary>
        public long VotingPower { get; set; }

        /// <summary>
        /// Voter's status:
        /// `none` - the voter did nothing
        /// `upvoted` - the voter upvoted at least one proposal
        /// `voted_yay` - the voter voted "yay"
        /// `voted_nay` - the voter voted "nay"
        /// `voted_pass` - the voter voted "pass"
        /// </summary>
        public string Status { get; set; }

        #region deprecated
        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int Rolls => (int)(VotingPower / 6_000_000_000);
        #endregion
    }
}
