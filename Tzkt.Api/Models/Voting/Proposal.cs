using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Api.Services.Metadata;

namespace Tzkt.Api.Models
{
    public class Proposal
    {
        public string Hash { get; set; }

        public Alias Initiator { get; set; }

        public int Period { get; set; }

        public int Upvotes { get; set; }

        public string Status { get; set; }

        public ProposalMetadata Metadata { get; set; }
    }
}
