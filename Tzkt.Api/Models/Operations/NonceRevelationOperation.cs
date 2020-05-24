using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class NonceRevelationOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `nonce_revelation` - are used by the blockchain to create randomness
        /// </summary>
        public override string Type => OpTypes.NonceRevelation;

        /// <summary>
        /// Unique ID of the operation, stored in the TzKT indexer database
        /// </summary>
        public override int Id { get; set; }

        /// <summary>
        /// The height of the block from the genesis block, in which the operation was included
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Datetime of the block, in which the operation was included (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Hash of the block, in which the operation was included
        /// </summary>
        public string Block { get; set; }

        /// <summary>
        /// Hash of the operation
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Information about the delegate (baker), who produced the block with the operation
        /// </summary>
        public Alias Baker { get; set; }

        /// <summary>
        /// Information about the delegate (baker), who revealed the nonce (sent the operation)
        /// </summary>
        public Alias Sender { get; set; }

        /// <summary>
        /// Block height of the block, where seed nonce hash is stored
        /// </summary>
        public int RevealedLevel { get; set; }
    }
}
