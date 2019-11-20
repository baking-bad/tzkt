using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class DelegateInfo
    {
        public string Alias { get; set; }

        public string Address { get; set; }

        public bool Active { get; set; }

        public DelegateInfo(Alias delegat, bool staked)
        {
            Active = staked;
            Alias = delegat.Name;
            Address = delegat.Address;
        }
    }
}
