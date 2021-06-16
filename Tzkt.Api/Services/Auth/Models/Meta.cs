using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Api.Services.Auth
{
    public class Meta
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }
        
        [JsonPropertyName("metadata")]
        public RawJson Metadata { get; set; }
    }
    
}