using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class BallotOperation : Operation
    {
        public override string Type => OpTypes.Ballot;

        public override int Id { get; set; }

        public int Level { get; set; }

        public DateTime Timestamp { get; set; }

        public string Block { get; set; }

        public string Hash { get; set; }

        public PeriodInfo Period { get; set; }

        public string Proposal { get; set; }

        public Alias Delegate { get; set; }

        public string Vote { get; set; }
    }
}
