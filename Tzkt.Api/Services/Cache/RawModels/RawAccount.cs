using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Api.Services.Cache
{
    public abstract class RawAccount
    {
        public int Id { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }

        public long Balance { get; set; }
        public int Counter { get; set; }
        public Operations Operations { get; set; }

        public int? DelegateId { get; set; }
        public int? DelegationLevel { get; set; }
        public bool Staked { get; set; }
    }
}
