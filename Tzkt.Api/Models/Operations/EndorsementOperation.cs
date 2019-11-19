using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class EndorsementOperation : IOperation
    {
        [JsonIgnore]
        public int Id { get; set; }

        public string Type => "endorsement";

        public int Level { get; set; }

        public DateTime Timestamp { get; set; }

        public string Hash { get; set; }

        public Alias Delegate { get; set; }

        public int Slots { get; set; }
    }
}
