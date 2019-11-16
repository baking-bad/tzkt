using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class ManagerInfo
    {
        [JsonPropertyName("alias")]
        public string Name { get; set; }

        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("publicKey")]
        public string PublicKey { get; set; }

        public ManagerInfo(Alias manager, string publicKey)
        {
            Name = manager.Name;
            Address = manager.Address;
            PublicKey = publicKey;
        }
    }
}
