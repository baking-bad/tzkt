using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Tzkt.Data.Models;

namespace Tzkt.Api.Models
{
    public class Contract : Account
    {
        public override string Type => AccountTypes.Contract;

        public string Kind { get; set; }

        public string Alias { get; set; }

        public string Address { get; set; }

        public long Balance { get; set; }

        public CreatorInfo Creator { get; set; }

        public ManagerInfo Manager { get; set; }

        public DelegateInfo Delegate { get; set; }

        public int NumContracts { get; set; }

        public int NumDelegations { get; set; }

        public int NumOriginations { get; set; }

        public int NumTransactions { get; set; }

        public int NumReveals { get; set; }

        public int NumSystem { get; set; }

        public int FirstActivity { get; set; }

        public int LastActivity { get; set; }

        public IEnumerable<RelatedContract> Contracts { get; set; }

        public IEnumerable<Operation> Operations { get; set; }
    }
}
