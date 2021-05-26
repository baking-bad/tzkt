using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TzKT_Client
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
        public JsonElement Metadata { get; set; }

        public string MetJson => Metadata.GetRawText();
    }
    
}