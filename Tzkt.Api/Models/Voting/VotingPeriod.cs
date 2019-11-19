using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class VotingPeriod
    {
        public string Kind { get; set; }

        public int FirstLevel { get; set; }

        public int LastLevel { get; set; }
    }
}
