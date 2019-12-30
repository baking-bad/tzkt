using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tzkt.Api.Services.Metadata;
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

        public int? DelegationLevel { get; set; }

        public DateTime? DelegationTime { get; set; }

        public int NumContracts { get; set; }

        public int NumDelegations { get; set; }

        public int NumOriginations { get; set; }

        public int NumTransactions { get; set; }

        public int NumReveals { get; set; }

        public int NumMigrations { get; set; }

        public int FirstActivity { get; set; }

        public DateTime FirstActivityTime { get; set; }

        public int LastActivity { get; set; }

        public DateTime LastActivityTime { get; set; }

        public IEnumerable<RelatedContract> Contracts { get; set; }

        public IEnumerable<Operation> Operations { get; set; }

        public AccountMetadata Metadata { get; set; }
    }
}
