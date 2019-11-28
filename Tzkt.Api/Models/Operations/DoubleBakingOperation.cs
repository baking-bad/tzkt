using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class DoubleBakingOperation : IOperation
    {
        public string Type => "double_baking";

        public int Id { get; set; }

        public int Level { get; set; }

        public DateTime Timestamp { get; set; }

        public string Hash { get; set; }

        public int AccusedLevel { get; set; }

        public Alias Accuser { get; set; }

        public long AccuserRewards { get; set; }

        public Alias Offender { get; set; }

        public long OffenderLostDeposits { get; set; }

        public long OffenderLostRewards { get; set; }

        public long OffenderLostFees { get; set; }
    }
}
