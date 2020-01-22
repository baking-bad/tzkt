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
        public string Protocol { get; set; }
        public DateTime Timestamp { get; set; }

        public int KnownLevel { get; set; }
        public DateTime LastSync { get; set; }
        
        public bool Synced => KnownLevel == Level;
    }
}
