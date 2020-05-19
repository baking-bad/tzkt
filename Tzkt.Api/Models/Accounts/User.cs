using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tzkt.Api.Services.Metadata;

namespace Tzkt.Api.Models
{
    public class User : Account
    {
        public override string Type => AccountTypes.User;

        /// <summary>
        /// Name of the project behind the account or account description
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Public key hash of the account
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Base58 representation of account's public key
        /// </summary>
        public string PublicKey { get; set; }
        
        /// <summary>
        /// Public key revelation status
        /// </summary>
        public bool Revealed { get; set; }

        /// <summary>
        /// Account balance
        /// </summary>
        public long Balance { get; set; }

        /// <summary>
        /// An account nonce which is used to prevent operation replay
        /// </summary>
        public int Counter { get; set; }

        /// <summary>
        /// Information about the current delegate of the account
        /// </summary>
        public DelegateInfo Delegate { get; set; }

        /// <summary>
        /// Block height of latest delegation
        /// </summary>
        public int? DelegationLevel { get; set; }

        /// <summary>
        /// Block datetime of latest delegation (ISO 8601, e.g. 2019-11-31)
        /// </summary>
        public DateTime? DelegationTime { get; set; }

        /// <summary>
        /// Number of contracts, related to the account
        /// </summary>
        public int NumContracts { get; set; }

        /// <summary>
        /// Number of account activation operations. Are used to activate accounts that were recommended allocations of
        /// tezos tokens for donations to the Tezos Foundation’s fundraiser
        /// </summary>
        public int NumActivations { get; set; }

        /// <summary>
        /// Number of delegation operations, related to the account
        /// </summary>
        public int NumDelegations { get; set; }

        /// <summary>
        /// Number of all origination (deployment / contract creation) operations, related to the account
        /// </summary>
        public int NumOriginations { get; set; }

        /// <summary>
        /// Number of all transaction (tez transfer) operations, related to the account
        /// </summary>
        public int NumTransactions { get; set; }

        /// <summary>
        /// Number of reveal (is used to reveal the public key associated with a tz1 address
        /// (implicit account/public key hash)) operations of the account
        /// </summary>
        public int NumReveals { get; set; }

        /// <summary>
        /// Number of migration (result of the context (database) migration during a protocol update) operations,
        /// related to the account (synthetic type) 
        /// </summary>
        public int NumMigrations { get; set; }

        /// <summary>
        /// Block height of the first operation, related to the account
        /// </summary>
        public int? FirstActivity { get; set; }

        /// <summary>
        /// Block datetime of the first operation, related to the account (ISO 8601, e.g. 2019-11-31)
        /// </summary>
        public DateTime? FirstActivityTime { get; set; }

        /// <summary>
        /// Block height of any last action, related to the account
        /// </summary>
        public int? LastActivity { get; set; }

        /// <summary>
        /// Block datetime of any last action, related to the account (ISO 8601, e.g. 2019-11-31)
        /// </summary>
        public DateTime? LastActivityTime { get; set; }

        /// <summary>
        /// List of contracts, related (originated or managed) to the account
        /// </summary>
        public IEnumerable<RelatedContract> Contracts { get; set; }

        /// <summary>
        /// List of all operations (synthetic type included), related to the account
        /// </summary>
        public IEnumerable<Operation> Operations { get; set; }

        /// <summary>
        /// Metadata of the account (alias, logo, website, contacts, etc)
        /// </summary>
        public AccountMetadata Metadata { get; set; }
    }
}
