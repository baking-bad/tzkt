using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class VotingEpoch
    {
        /// <summary>
        /// Index of the voting epoch, starting from zero
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The height of the block in which the epoch starts
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// The timestamp of the block in which the epoch starts
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The height of the block in which the epoch ends
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// The timestamp of the block in which the epoch ends
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Status of the voting epoch:
        /// `no_proposals` - there were no proposals proposed
        /// `voting` - there was at least one proposal and the voting is in progress
        /// `completed` - voting successfully completed and the proposal was accepted
        /// `failed` - voting was not completed due to either quorum or supermajority was not reached
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Voting periods in the epoch
        /// </summary>
        public IEnumerable<VotingPeriod> Periods { get; set; }

        /// <summary>
        /// Proposals pushed during the voting epoch (null, if there were no proposals).
        /// </summary>
        public IEnumerable<Proposal> Proposals { get; set; }
    }
}
