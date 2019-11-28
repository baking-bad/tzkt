using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class CreatorInfo
    {
        public string Type { get; set; }

        public string Alias { get; set; }

        public string Address { get; set; }

        public CreatorInfo(Alias manager, string type)
        {
            Type = type;
            Alias = manager.Name;
            Address = manager.Address;
        }
    }
}
