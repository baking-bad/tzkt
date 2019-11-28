using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class NonceRevelationOperation : IOperation
    {
        public string Type => "nonce_revelation";

        public int Id { get; set; }

        public int Level { get; set; }

        public DateTime Timestamp { get; set; }

        public string Hash { get; set; }

        public Alias Baker { get; set; }

        public Alias Sender { get; set; }

        public int RevealedLevel { get; set; }
    }
}
