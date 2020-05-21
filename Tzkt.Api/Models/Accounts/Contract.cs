using System;
using System.Collections.Generic;
using Tzkt.Api.Services.Metadata;

namespace Tzkt.Api.Models
{
    public class Contract : Account
    {
        /// <summary>
        /// Type of the account, `contract` - smart contract programmable account
        /// </summary>
        public override string Type => AccountTypes.Contract;

        /// <summary>
        /// Kind of the contract (`delegator_contract` or `smart_contract`),
        /// where `delegator_contract` - manager.tz smart contract for delegation purpose only
        /// </summary>
        public string Kind { get; set; }
        
        /// <summary>
        /// Name of the project behind the contract or contract description
        /// </summary>
        public string Alias { get; set; }
        
        /// <summary>
        /// Public key hash of the contract
        /// </summary>
        public string Address { get; set; }
        
        /// <summary>
        /// Contract balance (micro tez)
        /// </summary>
        public long Balance { get; set; }
        
        /// <summary>
        /// Information about the account, which has deployed the contract to the blockchain
        /// </summary>       
        public CreatorInfo Creator { get; set; }
        
        /// <summary>
        /// Information about the account, which was marked as a manager when contract was deployed to the blockchain
        /// </summary>
        public ManagerInfo Manager { get; set; }
        
        /// <summary>
        /// Information about the current delegate of the contract. `null` if it's not delegated
        /// </summary>
        public DelegateInfo Delegate { get; set; }

        /// <summary>
        /// Block height of latest delegation. `null` if it's not delegated
        /// </summary>
        public int? DelegationLevel { get; set; }

        /// <summary>
        /// Block datetime of latest delegation (ISO 8601, e.g. `2020-02-20T02:40:57Z`). `null` if it's not delegated
        /// </summary>
        public DateTime? DelegationTime { get; set; }

        /// <summary>
        /// Number of contracts, created (originated) and/or managed by the contract
        /// </summary>
        public int NumContracts { get; set; }

        /// <summary>
        /// Number of delegation operations of the contract
        /// </summary>
        public int NumDelegations { get; set; }

        /// <summary>
        /// Number of origination (deployment / contract creation) operations, related the contract
        /// </summary>
        public int NumOriginations { get; set; }

        /// <summary>
        /// Number of transaction (transfer) operations, related to the contract
        /// </summary>
        public int NumTransactions { get; set; }
    
        /// <summary>
        /// Number of reveal (is used to reveal the public key associated with an account) operations of the contract
        /// </summary>
        public int NumReveals { get; set; }

        /// <summary>
        /// Number of migration (result of the context (database) migration during a protocol update) operations
        /// related to the contract (synthetic type)
        /// </summary>
        public int NumMigrations { get; set; }

        /// <summary>
        /// Block height of the contract creation
        /// </summary>
        public int FirstActivity { get; set; }

        /// <summary>
        /// Block datetime of the contract creation (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime FirstActivityTime { get; set; }

        /// <summary>
        /// Height of the block in which the account state was changed last time
        /// </summary>
        public int LastActivity { get; set; }

        /// <summary>
        /// Datetime of the block in which the account state was changed last time (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime LastActivityTime { get; set; }

        
        /// <summary>
        /// List of contracts, related to the contract
        /// </summary>
        public IEnumerable<RelatedContract> Contracts { get; set; }

        /// <summary>
        /// List of all operations (synthetic type included), related to the contract
        /// </summary>
        public IEnumerable<Operation> Operations { get; set; }

        /// <summary>
        /// Metadata of the contract (alias, logo, website, contacts, etc)
        /// </summary>
        public AccountMetadata Metadata { get; set; }
    }
}