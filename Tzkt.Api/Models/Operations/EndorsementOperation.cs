using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class EndorsementOperation : IOperation
    {
        public string Type => "endorsement";

        public int Id { get; set; }

        public int Level { get; set; }

        public DateTime Timestamp { get; set; }

        public string Hash { get; set; }

        public Alias Delegate { get; set; }

        public int Slots { get; set; }

        public long Rewards { get; set; }
    }
}
