namespace Tzkt.Api.Models
{
    public class VoterSnapshot
    {
        /// <summary>
        /// Voter identity
        /// </summary>
        public required Alias Delegate { get; set; }

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
        public required string Status { get; set; }
    }
}
