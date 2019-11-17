using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class InternalDelegationOperation
    {
        public int Level { get; set; }

        public DateTime Timestamp { get; set; }

        public string Hash { get; set; }

        public Alias Sender { get; set; }

        public int Counter { get; set; }

        public int Nonce { get; set; }

        public int GasUsed { get; set; }

        public Alias Delegate { get; set; }

        public string Status { get; set; }
    }
}
