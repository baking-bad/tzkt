namespace Tzkt.Api.Models
{
    public class ProposalOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `proposal` - is used by bakers (delegates) to submit and/or upvote proposals to amend the protocol
        /// </summary>
        public override string Type => ActivityTypes.Proposal;

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
        /// Information about the proposal period for which the proposal was submitted (upvoted)
        /// </summary>
        public required PeriodInfo Period { get; set; }

        /// <summary>
        /// Information about the submitted (upvoted) proposal
        /// </summary>
        public required ProposalAlias Proposal { get; set; }

        /// <summary>
        /// Information about the baker (delegate), submitted (upvoted) the proposal operation
        /// </summary>
        public required Alias Delegate { get; set; }

        /// <summary>
        /// Baker's voting power
        /// </summary>
        public long VotingPower { get; set; }

        /// <summary>
        /// Indicates whether proposal upvote has already been pushed. Duplicated proposal operations are not counted when selecting proposal-winner.
        /// </summary>
        public bool Duplicated { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort? Quote { get; set; }
        #endregion
    }
}
