using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class PeriodInfo
    {
        public int Id { get; set; }

        public string Kind { get; set; }

        public int StartLevel { get; set; }

        public int EndLevel { get; set; }
    }
}
