using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tezzycat.Models
{
    public class AppState
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public DateTime Timestamp { get; set; }
        public string Protocol { get; set; }
        public string Hash { get; set; }
    }
}
