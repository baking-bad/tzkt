using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class Account
    {
        public string Type { get; set; }

        public string Alias { get; set; }

        public string Address { get; set; }

        public long Balance { get; set; }

        public DelegateInfo Delegate { get; set; }

        public int FirstActivity { get; set; }

        public int LastActivity { get; set; }
    }
}
