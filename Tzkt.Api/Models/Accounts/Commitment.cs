using System;

namespace Tzkt.Api.Models
{
    public class Commitment
    {
        /// <summary>
        /// Blinded address of the account
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Account balance to be activated
        /// </summary>
        public long Balance { get; set; }

        /// <summary>
        /// Flag showing whether the account has been activated or not.
        /// </summary>
        public bool Activated { get; set; }

        /// <summary>
        /// Level of the block at which the account has been activated. `null` if the account is not activated yet.
        /// </summary>
        public int? ActivationLevel { get; set; }

        /// <summary>
        /// Datetime of the block at which the account has been activated (ISO 8601, e.g. `2020-02-20T02:40:57Z`). `null` if the account is not activated yet.
        /// </summary>
        public DateTime? ActivationTime { get; set; }

        /// <summary>
        /// Info about activated account. `null` if the account is not activated yet.
        /// </summary>
        public Alias ActivatedAccount { get; set; }
    }
}
