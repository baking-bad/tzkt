using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Services.Cache
{
    public class RawState
    {
        public int Level { get; set; }

        public string Hash { get; set; }

        public string Protocol { get; set; }

        public DateTime Timestamp { get; set; }

        public int ManagerCounter { get; set; }
    }
}
