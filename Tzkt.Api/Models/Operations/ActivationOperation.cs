using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class ActivationOperation : IOperation
    {
        [JsonIgnore]
        public int Id { get; set; }

        public string Type => "activation";

        public int Level { get; set; }

        public DateTime Timestamp { get; set; }

        public string Hash { get; set; }

        public Alias Account { get; set; }

        public long Balance { get; set; }
    }
}
