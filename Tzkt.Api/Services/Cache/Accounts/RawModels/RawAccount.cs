using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Api.Services.Cache
{
    public abstract class RawAccount
    {
        public virtual string Type => "account";

        public int Id { get; set; }
        public string Address { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }

        public long Balance { get; set; }
        public int Counter { get; set; }

        public int Contracts { get; set; }
        public int DelegationsCount { get; set; }
        public int OriginationsCount { get; set; }
        public int TransactionsCount { get; set; }
        public int RevealsCount { get; set; }
        public int SystemOpsCount { get; set; }

        public int? DelegateId { get; set; }
        public int? DelegationLevel { get; set; }
        public bool Staked { get; set; }
    }
}
