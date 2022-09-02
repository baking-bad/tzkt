using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class VotingPeriod
    {
        /// <summary>
        /// Index of the voting period, starting from zero
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Index of the voting epoch, starting from zero
        /// </summary>
        public int Epoch { get; set; }

        /// <summary>
        /// The height of the block in which the period starts
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// The timestamp of the block in which the period starts
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The height of the block in which the period ends
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// The timestamp of the block in which the period ends
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Kind of the voting period:
        /// `proposal` - delegates can submit protocol amendment proposals using the proposal operation
        /// `exploration` -  bakers (delegates) may vote on the top-ranked proposal from the previous Proposal Period using the ballot operation
        /// `testing` - If the proposal is approved in the Exploration Period, the testing (or 'cooldown') period begins and bakers start testing the new protocol
        /// `promotion` - delegates can cast one vote to promote or not the tested proposal using the ballot operation
        /// `adoption` - after the proposal is actually accepted, the ecosystem has some time to prepare to the upgrade
        /// Learn more: https://tezos.gitlab.io/whitedoc/voting.html
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// Status of the voting period:
        /// `active` - means that the voting period is in progress
        /// `no_proposals` - means that there were no proposals during the voting period
        /// `no_quorum` - means that there was a voting but the quorum was not reached
        /// `no_supermajority` - means that there was a voting but the supermajority was not reached
        /// `success` - means that the period was finished with positive voting result
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Status of the governance dictator:
        /// `none` - means that there were no actions by the dictator 
        /// `abort` - means that the epoch was aborted by the dictator
        /// `reset` - means that the period was reset by the dictator
        /// `submit` - means that the dictator submitted a proposal
        /// </summary>
        public string Dictator { get; set; }

        /// <summary>
        /// The number of bakers on the voters list
        /// </summary>
        public int? TotalBakers { get; set; }

        /// <summary>
        /// Total voting power of bakers on the voters list
        /// </summary>
        public long? TotalVotingPower { get; set; }

        #region proposal
        /// <summary>
        /// Upvotes quorum percentage (only for proposal period)
        /// </summary>
        public double? UpvotesQuorum { get; set; }

        /// <summary>
        /// The number of proposals injected during the voting period (only for proposal period)
        /// </summary>
        public int? ProposalsCount { get; set; }

        /// <summary>
        /// This is how many upvotes (proposal operations) the most upvoted proposal has (only for proposal period)
        /// </summary>
        public int? TopUpvotes { get; set; }

        /// <summary>
        /// This is how much voting power the most upvoted proposal has (only for proposal period)
        /// </summary>
        public long? TopVotingPower { get; set; }
        #endregion

        #region ballot
        /// <summary>
        /// Ballots quorum percentage (only for exploration and promotion periods)
        /// </summary>
        public double? BallotsQuorum { get; set; }

        /// <summary>
        /// Supermajority percentage (only for exploration and promotion periods)
        /// </summary>
        public double? Supermajority { get; set; }

        /// <summary>
        /// The number of the ballots with "yay" vote (only for exploration and promotion periods)
        /// </summary>
        public int? YayBallots { get; set; }

        /// <summary>
        /// Total voting power of the ballots with "yay" vote (only for exploration and promotion periods)
        /// </summary>
        public long? YayVotingPower { get; set; }

        /// <summary>
        /// The number of the ballots with "nay" vote (only for exploration and promotion periods)
        /// </summary>
        public int? NayBallots { get; set; }

        /// <summary>
        /// Total voting power of the ballots with "nay" vote (only for exploration and promotion periods)
        /// </summary>
        public long? NayVotingPower { get; set; }

        /// <summary>
        /// The number of the ballots with "pass" vote (only for exploration and promotion periods)
        /// </summary>
        public int? PassBallots { get; set; }

        /// <summary>
        /// Total voting power of the ballots with "pass" vote (only for exploration and promotion periods)
        /// </summary>
        public long? PassVotingPower { get; set; }
        #endregion

        #region deprecated
        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int? TotalRolls => TotalVotingPower == null ? null : (int)(TotalVotingPower / 6_000_000_000);

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int? TopRolls => TopVotingPower == null ? null : (int)(TopVotingPower / 6_000_000_000);

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int? YayRolls => YayVotingPower == null ? null : (int)(YayVotingPower / 6_000_000_000);

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int? NayRolls => NayVotingPower == null ? null : (int)(NayVotingPower / 6_000_000_000);

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int? PassRolls => PassVotingPower == null ? null : (int)(PassVotingPower / 6_000_000_000);
        #endregion
    }
}
