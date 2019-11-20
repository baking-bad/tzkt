using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class User : IAccount
    {
        public string Type => "user";

        public string Alias { get; set; }

        public string Address { get; set; }

        public string PublicKey { get; set; }

        public long Balance { get; set; }

        public int Counter { get; set; }

        public DelegateInfo Delegate { get; set; }

        public int? FirstActivity { get; set; }

        public int? LastActivity { get; set; }
    }
}
