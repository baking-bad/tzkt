using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class BallotOperation
    {
        public int Level { get; set; }

        public DateTime Timestamp { get; set; }

        public string Hash { get; set; }

        public Alias Delegate { get; set; }

        public int Period { get; set; }

        public string Proposal { get; set; }

        public string Vote { get; set; }
    }
}
