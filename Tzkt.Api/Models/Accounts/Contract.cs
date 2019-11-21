using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Tzkt.Data.Models;

namespace Tzkt.Api.Models
{
    public class Contract : IAccount
    {
        public string Type => "contract";

        public string Kind { get; set; }

        public string Alias { get; set; }

        public string Address { get; set; }

        public long Balance { get; set; }

        public DelegateInfo Delegate { get; set; }

        public ManagerInfo Manager { get; set; }

        public int FirstActivity { get; set; }

        public int LastActivity { get; set; }

        public IEnumerable<IAccount> Contracts { get; set; }

        public IEnumerable<IOperation> Operations { get; set; }
    }
}
