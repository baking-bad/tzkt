using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class Contract
    {
        public string Kind { get; set; }

        public string Alias { get; set; }

        public string Address { get; set; }

        public long Balance { get; set; }

        public DelegateInfo Delegate { get; set; }

        public ManagerInfo Manager { get; set; }

        public int FirstActivity { get; set; }

        public int LastActivity { get; set; }
    }
}
