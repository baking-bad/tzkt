using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Services.Cache
{
    public class RawState
    {
        public int KnownHead { get; set; }

        public DateTime LastSync { get; set; }

        public int Level { get; set; }

        public string Hash { get; set; }

        public DateTime Timestamp { get; set; }

        public int ManagerCounter { get; set; }
    }
}
