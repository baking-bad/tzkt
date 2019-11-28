using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class ContractInfo
    {
        public string Kind { get; set; }

        public string Alias { get; set; }

        public string Address { get; set; }

        public ContractInfo(Alias manager, string kind)
        {
            Kind = kind;
            Alias = manager.Name;
            Address = manager.Address;
        }
    }
}
