using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Services.Cache
{
    public class RawContract : RawAccount
    {
        public override string Type => AccountTypes.Contract;

        public int Kind { get; set; }

        public int? CreatorId { get; set; }
        public int? ManagerId { get; set; }

        public string KindString => Kind switch
        {
            0 => "delegator_contract",
            1 => "smart_contract",
            _ => "unknown"
        };
    }
}
