using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class MigrationOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `migration` - result of the context (database) migration during a protocol update (synthetic type)
        /// </summary>
        public override string Type => OpTypes.Migration;

        /// <summary>
        /// Unique ID of the operation, stored in the TzKT indexer database
        /// </summary>
        public override long Id { get; set; }

        /// <summary>
        /// The height of the block from the genesis block, in which the operation was included
        /// </summary>
        public int Level { get; set; }

        //TODO Think about it
        /// <summary>
        /// Datetime of the block, in which the operation was included (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Hash of the block, in which the operation was included
        /// </summary>
        public string Block { get; set; }

        /// <summary>
        /// Kind of the migration 
        /// `bootstrap` - balance updates, included in the first block after genesis
        /// `activate_delegate` - registering a new baker (delegator) during protocol migration
        /// `airdrop` - airdrop of 1 micro tez during Babylon protocol upgrade
        /// `proposal_invoice` - invoice for creation a proposal for protocol upgrade
        /// `code_change` - changing contract scripts during Babylon protocol upgrade
        /// `origination` - implicit (hardcoded in the protocol) origination of liquidity baking contracts
        /// `subsidy` - liquidity baking subsidy
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// Information about the account whose balance has updated as a result of the operation
        /// </summary>
        public Alias Account { get; set; }

        /// <summary>
        /// The amount for which the operation updated the balance (micro tez)
        /// </summary>
        public long BalanceChange { get; set; }

        /// <summary>
        /// Contract storage after the migration converted to human-readable JSON. Note: you can configure storage format by setting `micheline` query parameter.
        /// </summary>
        public object Storage { get; set; }

        /// <summary>
        /// List of bigmap updates caused by the migration.
        /// </summary>
        public List<BigMapDiff> Diffs { get; set; }

        /// <summary>
        /// Number of token transfers produced by the operation, or `null` if there are no transfers
        /// </summary>
        public int? TokenTransfersCount { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion
    }
}
