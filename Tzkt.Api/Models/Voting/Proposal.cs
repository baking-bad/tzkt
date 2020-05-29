using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Api.Services.Metadata;

namespace Tzkt.Api.Models
{
    public class Proposal
    {
        /// <summary>
        /// Hash of the proposal, which representing a tarball of concatenated .ml/.mli source files
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Information about the baker (delegate) submitted the proposal
        /// </summary>
        public Alias Initiator { get; set; }

        /// <summary>
        /// Protocol-level voting period counter
        /// </summary>
        public int Period { get; set; }

        /// <summary>
        /// The total number of rolls of all the bakers (delegates) who upvoted the proposal
        /// </summary>
        public int Upvotes { get; set; }

        /// <summary>
        /// Status of the proposal
        /// `active` - the proposal in the active state
        /// `accepted` - accepted for protocol upgrade proposal
        /// `skipped` - the proposal didn't pass the Proposal Period
        /// `rejected` - the proposal didn't reach a quorum during the Exploration or Promotion Period
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Information about the proposal
        /// </summary>
        public ProposalMetadata Metadata { get; set; }
    }
}
