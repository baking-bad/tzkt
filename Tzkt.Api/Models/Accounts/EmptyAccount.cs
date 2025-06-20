﻿namespace Tzkt.Api.Models
{
    public class EmptyAccount : Account
    {
        /// <summary>
        /// Type of the account, `empty` - account hasn't appeared in the blockchain yet
        /// </summary>
        public override string Type => AccountTypes.Empty;

        /// <summary>
        /// Public key hash of the account
        /// </summary>
        public override required string Address { get; set; }

        /// <summary>
        /// An account nonce which is used to prevent operation replay
        /// </summary>
        public int Counter { get; set; }
    }
}
