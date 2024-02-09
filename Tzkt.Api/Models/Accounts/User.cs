﻿using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class User : Account
    {
        /// <summary>
        /// Internal MvKT id
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
        /// Amount of tx rollup commitment bonds (micro tez)
        /// </summary>
        public long RollupBonds { get; set; }

        /// <summary>
        /// Amount of smart rollup commitment bonds (micro tez)
        /// </summary>
        public long SmartRollupBonds { get; set; }

        /// <summary>
        /// Amount staked with the selected baker (micro tez).
        /// Like delegated amount, except for it is frozen and can be slashed.
        /// </summary>
        public long StakedBalance { get; set; }

        /// <summary>
        /// Amount of "pseudo-tokens" received after staking. These pseudotokens are used for unstaking.
        /// </summary>
        public long StakedPseudotokens { get; set; }

        /// <summary>
        /// Amount that was unstaked, but not yet finalized (i.e. it is still frozen) (micro tez)
        /// </summary>
        public long UnstakedBalance { get; set; }

        /// <summary>
        /// Information about the baker, for which there are pending unstake requests
        /// </summary>
        public Alias UnstakedBaker { get; set; }

        /// <summary>
        /// Amount lost due to inaccuracy of the economic protocol introduced in Oxford.
        /// This amount is literally lost, because it is no longer available for the account in any mean, but for some reason it is counted as delegated.
        /// </summary>
        public long LostBalance { get; set; }

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
        /// Number of smart rollups, created (originated) by the account
        /// </summary>
        public int SmartRollupsCount { get; set; }

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
        /// Number of tickets the account owns.
        /// </summary>
        public int ActiveTicketsCount { get; set; }

        /// <summary>
        /// Number of tickets the account ever owned.
        /// </summary>
        public int TicketBalancesCount { get; set; }

        /// <summary>
        /// Number of ticket transfers from/to the account.
        /// </summary>
        public int TicketTransfersCount { get; set; }

        /// <summary>
        /// Number of account activation operations. Are used to activate accounts that were recommended allocations of
        /// mavryk tokens for donations to the Mavryk fundraiser
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
        /// Number of `increase_paid_storage` operations sent by the account
        /// </summary>
        public int IncreasePaidStorageCount { get; set; }

        /// <summary>
        /// Number of `drain_delegate` operations related to the account
        /// </summary>
        public int DrainDelegateCount { get; set; }

        /// <summary>
        /// Number of `smart_rollup_add_messages` operations related to the account
        /// </summary>
        public int SmartRollupAddMessagesCount { get; set; }

        /// <summary>
        /// Number of `smart_rollup_cement` operations related to the account
        /// </summary>
        public int SmartRollupCementCount { get; set; }

        /// <summary>
        /// Number of `smart_rollup_execute_outbox_message` operations related to the account
        /// </summary>
        public int SmartRollupExecuteCount { get; set; }

        /// <summary>
        /// Number of `smart_rollup_originate` operations related to the account
        /// </summary>
        public int SmartRollupOriginateCount { get; set; }

        /// <summary>
        /// Number of `smart_rollup_publish` operations related to the account
        /// </summary>
        public int SmartRollupPublishCount { get; set; }

        /// <summary>
        /// Number of `smart_rollup_recover_bond` operations related to the account
        /// </summary>
        public int SmartRollupRecoverBondCount { get; set; }

        /// <summary>
        /// Number of `smart_rollup_refute` operations related to the account
        /// </summary>
        public int SmartRollupRefuteCount { get; set; }

        /// <summary>
        /// Number of smart rollup refutation games related to the account
        /// </summary>
        public int RefutationGamesCount { get; set; }

        /// <summary>
        /// Number of active smart rollup refutation games related to the account
        /// </summary>
        public int ActiveRefutationGamesCount { get; set; }

        /// <summary>
        /// Number of staking operations related to the account
        /// </summary>
        public int StakingOpsCount { get; set; }

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
