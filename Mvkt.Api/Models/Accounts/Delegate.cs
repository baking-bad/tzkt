﻿using NJsonSchema.Annotations;

namespace Mvkt.Api.Models
{
    public class Delegate : Account
    {
        /// <summary>
        /// Internal MvKT id
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
        /// Amount of tx rollup commitment bonds (micro tez)
        /// </summary>
        public long RollupBonds { get; set; }

        /// <summary>
        /// Amount of smart rollup commitment bonds (micro tez)
        /// </summary>
        public long SmartRollupBonds { get; set; }

        /// <summary>
        /// Amount staked from the own balance (micro tez).
        /// Like delegated amount, except for it is frozen and can be slashed.
        /// </summary>
        public long StakedBalance { get; set; }

        /// <summary>
        /// Amount of "pseudo-tokens" received after staking. These pseudotokens are used for unstaking.
        /// </summary>
        public long StakedPseudotokens { get; set; }

        /// <summary>
        /// Amount that was unstaked, but not yet finalized (i.e. it is still frozen) (micro tez).
        /// </summary>
        public long UnstakedBalance { get; set; }

        /// <summary>
        /// Information about the baker, for which there are pending unstake requests.
        /// </summary>
        public Alias UnstakedBaker { get; set; }

        /// <summary>
        /// Amount staked from external stakers (micro tez).
        /// Like delegated amount, except for it is frozen and can be slashed.
        /// </summary>
        public long ExternalStakedBalance { get; set; }

        /// <summary>
        /// Amount that was unstaked by external stakers, but not yet finalized (i.e. it is still frozen) (micro tez).
        /// </summary>
        public long ExternalUnstakedBalance { get; set; }

        /// <summary>
        /// Total staked balance, which is `stakedBalance + externalStakedBalance`.
        /// </summary>
        public long TotalStakedBalance { get; set; }

        /// <summary>
        /// Total amount of issued "pseudo-tokens". These pseudotokens are used for unstaking.
        /// </summary>
        public long IssuedPseudotokens { get; set; }

        /// <summary>
        /// Number of external stakers.
        /// </summary>
        public int StakersCount { get; set; }

        /// <summary>
        /// Amount lost due to inaccuracy of the economic protocol introduced in Oxford.
        /// This amount is literally lost, because it is no longer available for the account in any mean, but for some reason it is counted as delegated.
        /// </summary>
        public long LostBalance { get; set; }

        /// <summary>
        /// Configured max amount allowed to be locked as a security deposit (micro tez)
        /// </summary>
        public long? FrozenDepositLimit { get; set; }

        /// <summary>
        /// This parameter determines the maximum portion (millionth) of external stake by stakers over the baker's own staked funds.
        /// </summary>
        public long? LimitOfStakingOverBaking { get; set; }

        /// <summary>
        /// This parameter determines the fraction (billionth) of the rewards that accrue to the baker's liquid spendable balance — the remainder accrues to frozen stakes.
        /// </summary>
        public long? EdgeOfBakingOverStaking { get; set; }

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
        /// mavryk tokens for donations to the Mavryk fundraiser
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
        /// Number of `increase_paid_storage` operations sent by the account
        /// </summary>
        public int IncreasePaidStorageCount { get; set; }

        /// <summary>
        /// Number of `update_consensus_key` operations sent by the account
        /// </summary>
        public int UpdateConsensusKeyCount { get; set; }

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
        /// Number of autostaking operations related to the account
        /// </summary>
        public int AutostakingOpsCount { get; set; }

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
        /// Off-chain extras
        /// </summary>
        [JsonSchemaType(typeof(object), IsNullable = true)]
        public RawJson Extras { get; set; }

        /// <summary>
        /// Last seen baker's software
        /// </summary>
        public SoftwareAlias Software { get; set; }

        #region deprecated
        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long FrozenDeposit => StakedBalance;

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

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public RawJson Metadata { get; set; }
        #endregion
    }
}
