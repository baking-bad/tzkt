using System;

namespace Tzkt.Api.Models
{
    public class User : Account
    {
        /// <summary>
        /// Internal TzKT id
        /// </summary>
        public int Id { get; set; }

        /// Type of the account, `user` - simple wallet account
        public override string Type => AccountTypes.User;

        /// <summary>
        /// Public key hash of the account
        /// </summary>
        public override string Address { get; set; }

        /// <summary>
        /// Name of the project behind the account or account description
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Base58 representation of account's public key, revealed by the account
        /// </summary>
        public string PublicKey { get; set; }
        
        /// <summary>
        /// Public key revelation status. Unrevealed account can't send manager operation (transaction, origination etc.)
        /// </summary>
        public bool Revealed { get; set; }

        /// <summary>
        /// Account balance
        /// </summary>
        public long Balance { get; set; }

        /// <summary>
        /// Amount of rollup commitment bonds (micro tez)
        /// </summary>
        public long RollupBonds { get; set; }

        /// <summary>
        /// An account nonce which is used to prevent operation replay
        /// </summary>
        public int Counter { get; set; }

        /// <summary>
        /// Information about the current delegate of the account. `null` if it's not delegated
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
        /// Number of contracts, created (originated) and/or managed by the account
        /// </summary>
        public int NumContracts { get; set; }

        /// <summary>
        /// Number of tx rollups, created (originated) by the account
        /// </summary>
        public int RollupsCount { get; set; }

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
        /// Number of reveal (is used to reveal the public key associated with an account) operations of the contract
        /// </summary>
        public int NumReveals { get; set; }

        /// <summary>
        /// Number of register global constant operations sent by the account
        /// </summary>
        public int NumRegisterConstants { get; set; }

        /// <summary>
        /// Number of set deposits limit operations sent by the account
        /// </summary>
        public int NumSetDepositsLimits { get; set; }

        /// <summary>
        /// Number of migration (result of the context (database) migration during a protocol update) operations,
        /// related to the account (synthetic type) 
        /// </summary>
        public int NumMigrations { get; set; }

        /// <summary>
        /// Number of tx rollup origination operations sent by the account
        /// </summary>
        public int TxRollupOriginationCount { get; set; }

        /// <summary>
        /// Number of tx rollup submit batch operations sent by the account
        /// </summary>
        public int TxRollupSubmitBatchCount { get; set; }

        /// <summary>
        /// Number of tx rollup commit operations sent by the account
        /// </summary>
        public int TxRollupCommitCount { get; set; }

        /// <summary>
        /// Number of tx rollup return bond operations sent by the account
        /// </summary>
        public int TxRollupReturnBondCount { get; set; }

        /// <summary>
        /// Number of tx rollup finalize commitment operations sent by the account
        /// </summary>
        public int TxRollupFinalizeCommitmentCount { get; set; }

        /// <summary>
        /// Number of tx rollup remove commitment operations sent by the account
        /// </summary>
        public int TxRollupRemoveCommitmentCount { get; set; }

        /// <summary>
        /// Number of tx rollup rejection operations sent by the account
        /// </summary>
        public int TxRollupRejectionCount { get; set; }

        /// <summary>
        /// Number of tx rollup dispatch tickets operations sent by the account
        /// </summary>
        public int TxRollupDispatchTicketsCount { get; set; }

        /// <summary>
        /// Number of transfer ticket operations sent by the account
        /// </summary>
        public int TransferTicketCount { get; set; }

        /// <summary>
        /// Number of `increase_paid_storage` operations sent by the acount
        /// </summary>
        public int IncreasePaidStorageCount { get; set; }

        /// <summary>
        /// Block height of the first operation, related to the account
        /// </summary>
        public int? FirstActivity { get; set; }

        /// <summary>
        /// Block datetime of the first operation, related to the account (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime? FirstActivityTime { get; set; }

        /// <summary>
        /// Height of the block in which the account state was changed last time
        /// </summary>
        public int? LastActivity { get; set; }

        /// <summary>
        /// Datetime of the block in which the account state was changed last time (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime? LastActivityTime { get; set; }

        /// <summary>
        /// Metadata of the account (alias, logo, website, contacts, etc)
        /// </summary>
        public ProfileMetadata Metadata { get; set; }
    }
}
