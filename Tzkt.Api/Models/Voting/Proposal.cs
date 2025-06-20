﻿using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class Proposal
    {
        /// <summary>
        /// Hash of the proposal, which representing a tarball of concatenated .ml/.mli source files
        /// </summary>
        public required string Hash { get; set; }

        /// <summary>
        /// Information about the baker (delegate) submitted the proposal
        /// </summary>
        public required Alias Initiator { get; set; }

        /// <summary>
        /// The first voting period where the proposal was active
        /// </summary>
        public int FirstPeriod { get; set; }

        /// <summary>
        /// The last voting period where the proposal was active
        /// </summary>
        public int LastPeriod { get; set; }

        /// <summary>
        /// The voting epoch where the proposal was active
        /// </summary>
        public int Epoch { get; set; }

        /// <summary>
        /// The total number of upvotes (proposal operations)
        /// </summary>
        public int Upvotes { get; set; }

        /// <summary>
        /// The total voting power of bakers, upvoted the proposal
        /// </summary>
        public long VotingPower { get; set; }

        /// <summary>
        /// Status of the proposal
        /// `active` - the proposal in the active state
        /// `accepted` - the proposal was accepted
        /// `rejected` - the proposal was rejected due to too many "nay" ballots
        /// `skipped` - the proposal was skipped due to the quorum was not reached
        /// </summary>
        public required string Status { get; set; }

        /// <summary>
        /// Off-chain extras
        /// </summary>
        [JsonSchemaType(typeof(object), IsNullable = true)]
        public RawJson? Extras { get; set; }
    }
}
