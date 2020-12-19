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
        /// Voter's rolls snapshot (aka voting power)
        /// </summary>
        public int Rolls { get; set; }

        /// <summary>
        /// Voter's status:
        /// `none` - the voter did nothing
        /// `upvoted` - the voter upvoted at least one proposal
        /// `voted_yay` - the voter voted "yay"
        /// `voted_nay` - the voter voted "nay"
        /// `voted_pass` - the voter voted "pass"
        /// </summary>
        public string Status { get; set; }
    }
}
