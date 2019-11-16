using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class State
    {
        public int Level { get; set; }
        public string Hash { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
