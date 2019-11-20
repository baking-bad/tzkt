using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class DelegatorInfo
    {
        public string Alias { get; set; }

        public string Address { get; set; }

        public long Balance { get; set; }

        public int DelegationLevel { get; set; }
    }
}
