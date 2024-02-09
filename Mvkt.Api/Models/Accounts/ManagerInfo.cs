﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mvkt.Api.Models
{
    public class ManagerInfo
    {
        /// <summary>
        /// Name of the project behind the account or account description
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Public key hash of the account
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Base58 representation of account's public key, revealed by the account
        /// </summary>
        public string PublicKey { get; set; }
    }
}
