using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class ManagerInfo
    {
        public string Alias { get; set; }

        public string Address { get; set; }

        public string PublicKey { get; set; }

        public ManagerInfo(Alias manager, string publicKey)
        {
            Alias = manager.Name;
            Address = manager.Address;
            PublicKey = publicKey;
        }
    }
}
