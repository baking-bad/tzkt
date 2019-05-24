using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tezzycat.Models
{
    public class AppState
    {
        public int Id { get; set; }
        public int CurrentLevel { get; set; }
        public string CurrentHash { get; set; }
        public string CurrentProtocol { get; set; }
    }
}
