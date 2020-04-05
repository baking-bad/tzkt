using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class MigrationOperation : Operation
    {
        public override string Type => OpTypes.Migration;

        public override int Id { get; set; }

        public int Level { get; set; }

        public DateTime Timestamp { get; set; }

        public string Kind { get; set; }

        public Alias Account { get; set; }

        public long BalanceChange { get; set; }
    }
}
