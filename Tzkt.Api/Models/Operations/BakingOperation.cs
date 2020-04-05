using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class BakingOperation : Operation
    {
        public override string Type => OpTypes.Baking;

        public override int Id { get; set; }

        public int Level { get; set; }

        public DateTime Timestamp { get; set; }

        public Alias Baker { get; set; }

        public string Block { get; set; }

        public int Priority { get; set; }

        public long Reward { get; set; }

        public long Fees { get; set; }
    }
}
