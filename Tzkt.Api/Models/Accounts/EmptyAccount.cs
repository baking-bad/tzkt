using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class EmptyAccount : Account
    {
        public override string Type => "empty";

        public string Address { get; set; }

        public int Counter { get; set; }
    }
}
