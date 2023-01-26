using System;
using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class Rollup : Account
    {
        /// <summary>
        /// Internal TzKT id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Type of the account
        /// </summary>
        public override string Type => AccountTypes.Rollup;

        /// <summary>
        /// Address of the account
        /// </summary>
        public override string Address { get; set; }

        /// <summary>
        /// Name of the account
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Information about the account, which has deployed the rollup to the blockchain
        /// </summary>       
        public Alias Creator { get; set; }

        /// <summary>
        /// Amount of mutez locked as bonds
        /// </summary>
        public long RollupBonds { get; set; }

        /// <summary>
        /// Number of account tokens with non-zero balances
        /// </summary>
        public int ActiveTokensCount { get; set; }

        /// <summary>
        /// Number of tokens the account ever had
        /// </summary>
        public int TokenBalancesCount { get; set; }

        /// <summary>
        /// Number of token transfers from/to the account
        /// </summary>
        public int TokenTransfersCount { get; set; }

        /// <summary>
        /// Number of transaction operations related to the account
        /// </summary>
        public int NumTransactions { get; set; }

        /// <summary>
        /// Number of tx rollup origination operations related to the account
        /// </summary>
        public int TxRollupOriginationCount { get; set; }

        /// <summary>
        /// Number of tx rollup submit batch operations related to the account
        /// </summary>
        public int TxRollupSubmitBatchCount { get; set; }

        /// <summary>
        /// Number of tx rollup commit operations related to the account
        /// </summary>
        public int TxRollupCommitCount { get; set; }

        /// <summary>
        /// Number of tx rollup return bond operations related to the account
        /// </summary>
        public int TxRollupReturnBondCount { get; set; }

        /// <summary>
        /// Number of tx rollup finalize commitment operations related to the account
        /// </summary>
        public int TxRollupFinalizeCommitmentCount { get; set; }

        /// <summary>
        /// Number of tx rollup remove commitment operations related to the account
        /// </summary>
        public int TxRollupRemoveCommitmentCount { get; set; }

        /// <summary>
        /// Number of tx rollup rejection operations related to the account
        /// </summary>
        public int TxRollupRejectionCount { get; set; }

        /// <summary>
        /// Number of tx rollup dispatch tickets operations related to the account
        /// </summary>
        public int TxRollupDispatchTicketsCount { get; set; }

        /// <summary>
        /// Number of transfer ticket operations related to the account
        /// </summary>
        public int TransferTicketCount { get; set; }

        /// <summary>
        /// Block height at which the ghost contract appeared first time
        /// </summary>
        public int FirstActivity { get; set; }

        /// <summary>
        /// Block datetime at which the ghost contract appeared first time (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime FirstActivityTime { get; set; }

        /// <summary>
        /// Height of the block in which the ghost contract state was changed last time
        /// </summary>
        public int LastActivity { get; set; }

        /// <summary>
        /// Datetime of the block in which the ghost contract state was changed last time (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime LastActivityTime { get; set; }

        /// <summary>
        /// Off-chain extras
        /// </summary>
        [JsonSchemaType(typeof(object), IsNullable = true)]
        public RawJson Extras { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public RawJson Metadata { get; set; }
    }
}
