using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Api.Authentication
{
    public class SoftwareMetadata
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("commitDate")]
        public DateTime CommitDate { get; set; }

        [JsonPropertyName("commitHash")]
        public string CommitHash { get; set; }
    }


    public class Met
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }
        
        [JsonPropertyName("metadata")]
        public RawJson Metadata { get; set; }

        // public string MetJson => Metadata.GetRawText();
    }
    
}