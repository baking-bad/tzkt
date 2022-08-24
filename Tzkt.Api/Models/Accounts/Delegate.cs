using System;

namespace Tzkt.Api.Models
{
    public class Delegate : Account
    {
        /// <summary>
        /// Internal TzKT id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Type of the account, `delegate` - account, registered as a delegate (baker)
        /// </summary>
        public override string Type => AccountTypes.Delegate;

        /// <summary>
        /// Public key hash of the delegate (baker)
        /// </summary>
        public override string Address { get; set; }

        /// <summary>
        /// Delegation status (`true` - active, `false` - deactivated)
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Name of the baking service
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Public key of the delegate (baker)
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Public key revelation status. Unrevealed account can't send manager operation (transaction, origination etc.)
        /// </summary>
        public bool Revealed { get; set; }

        /// <summary>
        /// Total balance of the delegate (baker), including spendable and frozen funds (micro tez)
        /// </summary>
        public long Balance { get; set; }

        /// <summary>
        /// Amount of rollup commitment bonds (micro tez)
        /// </summary>
        public long RollupBonds { get; set; }

        /// <summary>
        /// Amount of security deposit, currently locked for baked (produced) blocks and (or) given endorsements (micro tez)
        /// </summary>
        public long FrozenDeposit { get; set; }

        /// <summary>
        /// Configured max amount allowed to be locked as a security deposit (micro tez)
        /// </summary>
        public long? FrozenDepositLimit { get; set; }

        /// <summary>
        /// An account nonce which is used to prevent operation replay
        /// </summary>
        public int Counter { get; set; }

        /// <summary>
        /// Block height when delegate (baker) was registered as a baker last time
        /// </summary>
        public int ActivationLevel { get; set; }

        /// <summary>
        /// Block datetime when delegate (baker) was registered as a baker last time (ISO 8601, e.g. 2019-11-31)
        /// </summary>
        public DateTime ActivationTime { get; set; }

        /// <summary>
        /// Block height when delegate (baker) was deactivated as a baker because of lack of funds or inactivity
        /// </summary>
        public int? DeactivationLevel { get; set; }

        /// <summary>
        /// Block datetime when delegate (baker) was deactivated as a baker because of lack of funds or inactivity (ISO 8601, e.g. 2019-11-31)
        /// </summary>
        public DateTime? DeactivationTime { get; set; }

        /// <summary>
        /// Baker's own balance plus delegated balance (micro tez)
        /// </summary>
        public long StakingBalance { get; set; }

        /// <summary>
        /// Total amount delegated to the baker (micro tez)
        /// </summary>
        public long DelegatedBalance { get; set; }

        /// <summary>
        /// Number of contracts, created (originated) and/or managed by the delegate (baker)
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
        /// Number of current delegators (accounts, delegated their funds) of the delegate (baker)
        /// </summary>
        public int NumDelegators { get; set; }

        /// <summary>
        /// Number of baked (validated) blocks all the time by the delegate (baker)
        /// </summary>
        public int NumBlocks { get; set; }

        /// <summary>
        /// Number of given endorsements (approvals) by the delegate (baker)
        /// </summary>
        public int NumEndorsements { get; set; }

        /// <summary>
        /// Number of given preendorsements (approvals) by the delegate (baker)
        /// </summary>
        public int NumPreendorsements { get; set; }

        /// <summary>
        /// Number of submitted by the delegate ballots during a voting period
        /// </summary>
        public int NumBallots { get; set; }

        /// <summary>
        /// Number of submitted (upvoted) by the delegate proposals during a proposal period
        /// </summary>
        public int NumProposals { get; set; }

        /// <summary>
        /// Number of account activation operations. Are used to activate accounts that were recommended allocations of
        /// tezos tokens for donations to the Tezos Foundation’s fundraiser
        /// </summary>
        public int NumActivations { get; set; }

        /// <summary>
        /// Number of double baking (baking two different blocks at the same height) evidence operations,
        /// included in blocks, baked (validated) by the delegate
        /// </summary>
        public int NumDoubleBaking { get; set; }

        /// <summary>
        /// Number of double endorsement (endorsing two different blocks at the same block height) evidence operations,
        /// included in blocks, baked (validated) by the delegate
        /// </summary>
        public int NumDoubleEndorsing { get; set; }

        /// <summary>
        /// Number of double preendorsement (preendorsing two different blocks at the same block height) evidence operations,
        /// included in blocks, baked (validated) by the delegate
        /// </summary>
        public int NumDoublePreendorsing { get; set; }

        /// <summary>
        /// Number of seed nonce revelation (are used by the blockchain to create randomness) operations provided by the delegate
        /// </summary>
        public int NumNonceRevelations { get; set; }

        /// <summary>
        /// Number of `vdf_revelation` operations included into blocks by the delegate
        /// </summary>
        public int VdfRevelationsCount { get; set; }

        /// <summary>
        /// Number of operations for all time in which rewards were lost due to unrevealed seed nonces by the delegate (synthetic type)
        /// </summary>
        public int NumRevelationPenalties { get; set; }

        /// <summary>
        /// Number of endorsing rewards received at the end of cycles (synthetic type)
        /// </summary>
        public int NumEndorsingRewards { get; set; }

        /// <summary>
        /// Number of all delegation related operations (new delegator, left delegator, registration as a baker),
        /// related to the delegate (baker) 
        /// </summary>
        public int NumDelegations { get; set; }

        /// <summary>
        /// Number of all origination (deployment / contract creation) operations, related to the delegate (baker)
        /// </summary>
        public int NumOriginations { get; set; }

        /// <summary>
        /// Number of all transaction (tez transfer) operations, related to the delegate (baker)
        /// </summary>
        public int NumTransactions { get; set; }

        /// <summary>
        /// Number of reveal (is used to reveal the public key associated with an account) operations of the delegate (baker)
        /// </summary>
        public int NumReveals { get; set; }

        /// <summary>
        /// Number of register global constant operations sent by the baker
        /// </summary>
        public int NumRegisterConstants { get; set; }

        /// <summary>
        /// Number of set deposits limit operations sent by the baker
        /// </summary>
        public int NumSetDepositsLimits { get; set; }

        /// <summary>
        /// Number of migration (result of the context (database) migration during a protocol update) operations,
        /// related to the delegate (synthetic type) 
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
        /// Block height of the first operation, related to the delegate (baker)
        /// </summary>
        public int FirstActivity { get; set; }

        /// <summary>
        /// Block datetime of the first operation, related to the delegate (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
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
        /// Metadata of the delegate (alias, logo, website, contacts, etc)
        /// </summary>
        public ProfileMetadata Metadata { get; set; }

        /// <summary>
        /// Last seen baker's software
        /// </summary>
        public SoftwareAlias Software { get; set; }

        #region deprecated
        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long FrozenDeposits => FrozenDeposit;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long FrozenRewards => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long FrozenFees => 0;
        #endregion
    }
}
