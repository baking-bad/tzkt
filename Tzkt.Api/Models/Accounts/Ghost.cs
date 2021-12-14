using System;

namespace Tzkt.Api.Models
{
    public class Ghost : Account
    {
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
        /// Metadata of the ghost contract (alias, logo, website, contacts, etc)
        /// </summary>
        public AccountMetadata Metadata { get; set; }
    }
}
