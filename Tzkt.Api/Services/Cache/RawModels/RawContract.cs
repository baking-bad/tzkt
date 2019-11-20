using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Services.Cache
{
    public class RawContract : RawAccount
    {
        public int Kind { get; set; }
        public int ManagerId { get; set; }
    }
}
