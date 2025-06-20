﻿namespace Tzkt.Api.Models
{
    public class DelegateInfo
    {
        /// <summary>
        /// Name of the baking service
        /// </summary>
        public string? Alias { get; set; }

        /// <summary>
        /// Public key hash of the delegate (baker)
        /// </summary>
        public required string Address { get; set; }

        /// <summary>
        /// Delegation status (`true` - active, `false` - deactivated)
        /// </summary>
        public bool Active { get; set; }
    }
}
