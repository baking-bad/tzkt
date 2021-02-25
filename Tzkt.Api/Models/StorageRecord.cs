using System;
using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class StorageRecord<T>
    {
        /// <summary>
        /// Id of the record that can be used for pagination
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Level at which the storage value was taken
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Timestamp at which the storage value was taken
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Operation that caused the storage change
        /// </summary>
        public SourceOperation<T> Operation { get; set; }

        /// <summary>
        /// New storage value
        /// </summary>
        [JsonSchemaType(typeof(object))]
        public T Value { get; set; }
    }

    public class SourceOperation<T>
    {
        /// <summary>
        /// Operation type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Operation hash
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Operation counter (null in case of synthetic operations)
        /// </summary>
        public int? Counter { get; set; }

        /// <summary>
        /// Operation nonce (null in case of non-internal or synthetic operations)
        /// </summary>
        public int? Nonce { get; set; }

        /// <summary>
        /// Transaction parameter, including called entrypoint and value passed to the entrypoint.
        /// </summary>
        public SourceOperationParameter<T> Parameter { get; set; }
    }

    public class SourceOperationParameter<T>
    {
        /// <summary>
        /// Called entrypoint
        /// </summary>
        public string Entrypoint { get; set; }

        /// <summary>
        /// Value passed to the entrypoint
        /// </summary>
        [JsonSchemaType(typeof(object))]
        public T Value { get; set; }
    }
}
