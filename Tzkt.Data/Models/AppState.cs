using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Data.Models
{
    public class AppState
    {
        public int Id { get; set; }
        public bool Synced { get; set; }

        public int Level { get; set; }
        public DateTime Timestamp { get; set; }
        public string Protocol { get; set; }
        public string Hash { get; set; }
    }
}
