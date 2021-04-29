using System;

namespace Tzkt.Api.Models
{
    public class StorageRecord
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
        public SourceOperation Operation { get; set; }

        /// <summary>
        /// New storage value
        /// </summary>
        public object Value { get; set; }
    }

    public class SourceOperation
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
        public TxParameter Parameter { get; set; }
    }
}
