using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class RevelationPenaltyOperation : Operation
    {
        public override string Type => OpTypes.RevelationPenalty;

        public override int Id { get; set; }

        public int Level { get; set; }

        public DateTime Timestamp { get; set; }

        public string Block { get; set; }

        public Alias Baker { get; set; }

        public int MissedLevel { get; set; }

        public long LostReward { get; set; }

        public long LostFees { get; set; }
    }
}
