namespace Tzkt.Api.Models
{
    public class BallotOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `ballot` - is used to vote for a proposal in a given voting cycle
        /// </summary>
        public override string Type => ActivityTypes.Ballot;

        /// <summary>
        /// Unique ID of the operation, stored in the TzKT indexer database
        /// </summary>
        public override long Id { get; set; }

        /// <summary>
        /// The height of the block from the genesis block, in which the operation was included
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Datetime of the block, in which the operation was included (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Hash of the block, in which the operation was included
        /// </summary>
        public required string Block { get; set; }

        /// <summary>
        /// Hash of the operation
        /// </summary>
        public required string Hash { get; set; }

        /// <summary>
        /// Information about the voting period for which the ballot was submitted
        /// </summary>
        public required PeriodInfo Period { get; set; }

        /// <summary>
        /// Information about the proposal for which ballot was submitted
        /// </summary>
        public required ProposalAlias Proposal { get; set; }

        /// <summary>
        /// Information about the delegate (baker), submitted the ballot
        /// </summary>
        public required Alias Delegate { get; set; }

        /// <summary>
        /// Baker's voting power
        /// </summary>
        public long VotingPower { get; set; }

        /// <summary>
        /// Vote, given in the ballot (`yay`, `nay`, or `pass`)
        /// </summary>
        public required string Vote { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort? Quote { get; set; }
        #endregion
    }
}
