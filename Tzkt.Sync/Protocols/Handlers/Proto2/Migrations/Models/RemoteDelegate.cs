using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto2.Migrations
{
    class RemoteDelegate
    {
        [JsonPropertyName("delegated_contracts")]
        public List<string> Delegators { get; set; }
    }
}
