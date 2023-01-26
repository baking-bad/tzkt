using System;
using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class Ghost : Account
    {
        /// <summary>
        /// Internal TzKT id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Type of the account, `ghost` - contract that has been met among token holders, but hasn't been originated
        /// </summary>
        public override string Type => AccountTypes.Ghost;

        /// <summary>
        /// Address of the contract
        /// </summary>
        public override string Address { get; set; }

        /// <summary>
        /// Name of the ghost contract
        /// </summary>
        public string Alias { get; set; }

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
