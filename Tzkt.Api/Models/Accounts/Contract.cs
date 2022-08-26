using System;
using System.Collections.Generic;

namespace Tzkt.Api.Models
{
    public class Contract : Account
    {
        /// <summary>
        /// Internal TzKT id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Type of the account, `contract` - smart contract programmable account
        /// </summary>
        public override string Type => AccountTypes.Contract;

        /// <summary>
        /// Public key hash of the contract
        /// </summary>
        public override string Address { get; set; }

        /// <summary>
        /// Kind of the contract (`delegator_contract` or `smart_contract`),
        /// where `delegator_contract` - manager.tz smart contract for delegation purpose only
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// List of implemented standards (TZIPs)
        /// </summary>
        public IEnumerable<string> Tzips { get; set; }
        
        /// <summary>
        /// Name of the project behind the contract or contract description
        /// </summary>
        public string Alias { get; set; }
        
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
        /// Number of account tokens with non-zero balances
        /// </summary>
        public int ActiveTokensCount { get; set; }

        /// <summary>
        /// Number of tokens minted in the contract
        /// </summary>
        public int TokensCount { get; set; }

        /// <summary>
        /// Number of tokens the account ever had
        /// </summary>
        public int TokenBalancesCount { get; set; }

        /// <summary>
        /// Number of token transfers from/to the account
        /// </summary>
        public int TokenTransfersCount { get; set; }

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
        /// Number of transfer ticket operations related to the contract
        /// </summary>
        public int TransferTicketCount { get; set; }

        /// <summary>
        /// Number of `increase_paid_storage` operations related to the contract
        /// </summary>
        public int IncreasePaidStorageCount { get; set; }

        /// <summary>
        /// Number of events produced by the contract
        /// </summary>
        public int EventsCount { get; set; }

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
        /// Contract storage value. Omitted by default. Use `?includeStorage=true` to include it in response.
        /// </summary>
        public object Storage { get; set; }

        /// <summary>
        /// 32-bit hash of the contract parameter and storage types.
        /// This field can be used for searching similar contracts (which have the same interface).
        /// </summary>
        public int TypeHash { get; set; }

        /// <summary>
        /// 32-bit hash of the contract code.
        /// This field can be used for searching same contracts (which have the same script).
        /// </summary>
        public int CodeHash { get; set; }

        /// <summary>
        /// Metadata of the contract (alias, logo, website, contacts, etc)
        /// </summary>
        public ProfileMetadata Metadata { get; set; }
    }
}
