using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class DelegateInfo
    {
        [JsonPropertyName("alias")]
        public string Name { get; set; }

        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        public DelegateInfo(Alias delegat, bool staked)
        {
            Active = staked;
            Name = delegat.Name;
            Address = delegat.Address;
        }
    }
}
