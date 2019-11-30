using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class User : Account
    {
        public override string Type => AccountTypes.User;

        public string Alias { get; set; }

        public string Address { get; set; }

        public string PublicKey { get; set; }

        public long Balance { get; set; }

        public int Counter { get; set; }

        public DelegateInfo Delegate { get; set; }

        public int NumContracts { get; set; }

        public int NumActivations { get; set; }

        public int NumDelegations { get; set; }

        public int NumOriginations { get; set; }

        public int NumTransactions { get; set; }

        public int NumReveals { get; set; }

        public int NumSystem { get; set; }

        public int? FirstActivity { get; set; }

        public DateTime? FirstActivityTime { get; set; }

        public int? LastActivity { get; set; }

        public DateTime? LastActivityTime { get; set; }

        public IEnumerable<RelatedContract> Contracts { get; set; }

        public IEnumerable<Operation> Operations { get; set; }
    }
}
