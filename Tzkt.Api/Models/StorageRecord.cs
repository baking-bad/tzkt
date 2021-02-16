using System;
using System.Text.Json.Serialization;
using NJsonSchema.Annotations;
using Netezos.Encoding;

namespace Tzkt.Api.Models
{
    public class StorageRecord
    {
        public int Id { get; set; }

        public int Level { get; set; }

        public DateTime Timestamp { get; set; }

        public SourceOperation Operation { get; set; }

        [JsonConverter(typeof(RawJsonConverter))]
        [JsonSchemaType(typeof(object))]
        public string Value { get; set; }
    }

    public class SourceOperation
    {
        public string Type { get; set; }

        public string Hash { get; set; }

        public int? Counter { get; set; }

        public int? Nonce { get; set; }

        public string Entrypoint { get; set; }

        [JsonConverter(typeof(RawJsonConverter))]
        [JsonSchemaType(typeof(object))]
        public string Params { get; set; }
    }

    public class RawStorageRecord
    {
        public int Id { get; set; }

        public int Level { get; set; }

        public DateTime Timestamp { get; set; }

        public SourceOperationRaw Operation { get; set; }

        public IMicheline Value { get; set; }
    }

    public class SourceOperationRaw
    {
        public string Type { get; set; }

        public string Hash { get; set; }

        public int? Counter { get; set; }

        public int? Nonce { get; set; }

        public string Entrypoint { get; set; }

        public IMicheline Params { get; set; }
    }
}
