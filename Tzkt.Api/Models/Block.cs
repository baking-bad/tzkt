namespace Tzkt.Api.Models
{
    public class Block
    {
        /// <summary>
        /// Index of the cycle
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// Height of the block from the genesis
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Block hash
        /// </summary>
        public required string Hash { get; set; }
        
        /// <summary>
        /// Datetime at which the block is claimed to have been created (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Protocol code, representing a number of protocol changes since genesis (mod 256, but `-1` for the genesis block)
        /// </summary>
        public int Proto { get; set; }

        /// <summary>
        /// Round at which the block payload was proposed
        /// </summary>
        public int PayloadRound { get; set; }

        /// <summary>
        /// Round at which the block was produced
        /// </summary>
        public int BlockRound { get; set; }

        /// <summary>
        /// Number of attestations (slots), included into the block
        /// </summary>
        public int Validations { get; set; }

        /// <summary>
        /// Security deposit frozen on the baker's account for producing the block (micro tez)
        /// </summary>
        public long Deposit { get; set; }

        /// <summary>
        /// Portion of fixed reward, corresponding to delegated stake, paid to payload proposer's liquid balance (micro tez)
        /// (it is not frozen and can be spent immediately).
        /// </summary>
        public long RewardDelegated { get; set; }

        /// <summary>
        /// Portion of fixed reward, corresponding to baker's own stake, paid to payload proposer's own staked balance (micro tez)
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long RewardStakedOwn { get; set; }

        /// <summary>
        /// Portion of fixed reward, corresponding to baker's edge from external stake, paid to payload proposer's own staked balance (micro tez)
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long RewardStakedEdge { get; set; }

        /// <summary>
        /// Portion of fixed reward, corresponding to baker's external stake, paid to payload proposer's external staked balance (micro tez)
        /// (it is frozen and belongs to baker's stakers).
        /// </summary>
        public long RewardStakedShared { get; set; }

        /// <summary>
        /// Portion of bonus reward, corresponding to delegated stake, paid to block producer's liquid balance (micro tez)
        /// (it is not frozen and can be spent immediately).
        /// </summary>
        public long BonusDelegated { get; set; }

        /// <summary>
        /// Portion of bonus reward, corresponding to baker's own stake, paid to block producer's own staked balance (micro tez)
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long BonusStakedOwn { get; set; }

        /// <summary>
        /// Portion of bonus reward, corresponding to baker's edge from external stake, paid to block producer's own staked balance (micro tez)
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long BonusStakedEdge { get; set; }

        /// <summary>
        /// Portion of fixed reward, corresponding to baker's external stake, paid to block producer's external staked balance (micro tez)
        /// (it is frozen and belongs to baker's stakers).
        /// </summary>
        public long BonusStakedShared { get; set; }

        /// <summary>
        /// Total fee gathered from operations, included into the block
        /// </summary>
        public long Fees { get; set; }

        /// <summary>
        /// Status of the seed nonce revelation
        /// `true` - seed nonce revealed
        /// `false` - there's no `seed_nonce_hash` in the block or seed nonce revelation has missed
        /// </summary>
        public bool NonceRevealed { get; set; }

        /// <summary>
        /// Baker who proposed the block payload
        /// </summary>
        public Alias? Proposer { get; set; }

        /// <summary>
        /// Baker who produced the block
        /// </summary>
        public Alias? Producer { get; set; }

        /// <summary>
        /// Information about baker's software
        /// </summary>
        public SoftwareAlias? Software { get; set; }

        /// <summary>
        /// Liquidity baking toggle (`true` if enabled, `false` if disabled, or `null` if the baker says 'pass')
        /// </summary>
        public bool? LBToggle { get; set; }

        /// <summary>
        /// Liquidity baking escape EMA value with precision of 1000000 for integer computation
        /// </summary>
        public int LBToggleEma { get; set; }

        /// <summary>
        /// Adaptive issuance toggle (`true` if enabled, `false` if disabled, or `null` if the baker says 'pass')
        /// </summary>
        public bool? AIToggle { get; set; }

        /// <summary>
        /// Adaptive issuance EMA value with precision of 1000000 for integer computation
        /// </summary>
        public int AIToggleEma { get; set; }

        #region operations
        /// <summary>
        /// List of attestation (is operation, which specifies the head of the chain as seen by the attester of a given slot)
        /// operations, included in the block
        /// </summary>
        public IEnumerable<AttestationOperation>? Attestations { get; set; }

        /// <summary>
        /// List of preattestation operations, included in the block
        /// </summary>
        public IEnumerable<PreattestationOperation>? Preattestations { get; set; }

        /// <summary>
        /// List of proposal (is used by bakers (delegates) to submit and/or upvote proposals to amend the protocol)
        /// operations, included in the block
        /// </summary>
        public IEnumerable<ProposalOperation>? Proposals { get; set; }
        
        /// <summary>
        /// List of ballot (is used to vote for a proposal in a given voting cycle) operations, included in the block
        /// </summary>
        public IEnumerable<BallotOperation>? Ballots { get; set; }

        /// <summary>
        /// List of activation (is used to activate accounts that were recommended allocations of
        /// tezos tokens for donations to the Tezos Foundation’s fundraiser) operations, included in the block
        /// </summary>
        public IEnumerable<ActivationOperation>? Activations { get; set; }

        /// <summary>
        /// List of dal entrapment evidence operations, included in the block
        /// </summary>
        public IEnumerable<DalEntrapmentEvidenceOperation>? DalEntrapmentEvidenceOps { get; set; }
        
        /// <summary>
        /// List of double baking evidence (is used by bakers to provide evidence of double baking (baking two different
        /// blocks at the same height) by a baker) operations, included in the block
        /// </summary>
        public IEnumerable<DoubleBakingOperation>? DoubleBaking { get; set; }

        /// <summary>
        /// List of double consensus evidence (is used by bakers to provide evidence of double (pre)attestation
        /// ((pre)attestation of two different blocks at the same block height) by a baker) operations, included in the block
        /// </summary>
        public IEnumerable<DoubleConsensusOperation>? DoubleConsensus { get; set; }

        /// <summary>
        /// List of nonce revelation (used by the blockchain to create randomness) operations, included in the block
        /// </summary>
        public IEnumerable<NonceRevelationOperation>? NonceRevelations { get; set; }

        /// <summary>
        /// List of vdf revelation (used by the blockchain to create randomness) operations, included in the block
        /// </summary>
        public IEnumerable<VdfRevelationOperation>? VdfRevelations { get; set; }

        /// <summary>
        /// List of delegation (is used to delegate funds to a delegate (an implicit account registered as a baker))
        /// operations, included in the block
        /// </summary>
        public IEnumerable<DelegationOperation>? Delegations { get; set; }
        
        /// <summary>
        /// List of origination (deployment / contract creation ) operations, included in the block
        /// </summary>
        public IEnumerable<OriginationOperation>? Originations { get; set; }
        
        /// <summary>
        /// List of transaction (is a standard operation used to transfer tezos tokens to an account)
        /// operations, included in the block
        /// </summary>
        public IEnumerable<TransactionOperation>? Transactions { get; set; }
        
        /// <summary>
        /// List of reveal (is used to reveal the public key associated with an account) operations, included in the block
        /// </summary>
        public IEnumerable<RevealOperation>? Reveals { get; set; }

        /// <summary>
        /// List of register global constant operations, included in the block
        /// </summary>
        public IEnumerable<RegisterConstantOperation>? RegisterConstants { get; set; }

        /// <summary>
        /// List of set deposits limit operations, included in the block
        /// </summary>
        public IEnumerable<SetDepositsLimitOperation>? SetDepositsLimits { get; set; }

        /// <summary>
        /// List of transfer ticket operations, included in the block
        /// </summary>
        public IEnumerable<TransferTicketOperation>? TransferTicketOps { get; set; }

        /// <summary>
        /// List of tx rollup commit operations, included in the block
        /// </summary>
        public IEnumerable<TxRollupCommitOperation>? TxRollupCommitOps { get; set; }

        /// <summary>
        /// List of tx rollup dispatch tickets operations, included in the block
        /// </summary>
        public IEnumerable<TxRollupDispatchTicketsOperation>? TxRollupDispatchTicketsOps { get; set; }

        /// <summary>
        /// List of tx rollup finalize commitment operations, included in the block
        /// </summary>
        public IEnumerable<TxRollupFinalizeCommitmentOperation>? TxRollupFinalizeCommitmentOps { get; set; }

        /// <summary>
        /// List of tx rollup origination operations, included in the block
        /// </summary>
        public IEnumerable<TxRollupOriginationOperation>? TxRollupOriginationOps { get; set; }

        /// <summary>
        /// List of tx rollup rejection operations, included in the block
        /// </summary>
        public IEnumerable<TxRollupRejectionOperation>? TxRollupRejectionOps { get; set; }

        /// <summary>
        /// List of tx rollup remove commitment operations, included in the block
        /// </summary>
        public IEnumerable<TxRollupRemoveCommitmentOperation>? TxRollupRemoveCommitmentOps { get; set; }

        /// <summary>
        /// List of tx rollup return bond operations, included in the block
        /// </summary>
        public IEnumerable<TxRollupReturnBondOperation>? TxRollupReturnBondOps { get; set; }

        /// <summary>
        /// List of tx rollup submit batch operations, included in the block
        /// </summary>
        public IEnumerable<TxRollupSubmitBatchOperation>? TxRollupSubmitBatchOps { get; set; }

        /// <summary>
        /// List of increase paid storage operations, included in the block
        /// </summary>
        public IEnumerable<IncreasePaidStorageOperation>? IncreasePaidStorageOps { get; set; }

        /// <summary>
        /// List of update secondary key operations, included in the block
        /// </summary>
        public IEnumerable<UpdateSecondaryKeyOperation>? UpdateSecondaryKeyOps { get; set; }

        /// <summary>
        /// List of drain delegate operations, included in the block
        /// </summary>
        public IEnumerable<DrainDelegateOperation>? DrainDelegateOps { get; set; }

        /// <summary>
        /// List of smart rollup add messages operations, included in the block
        /// </summary>
        public IEnumerable<SmartRollupAddMessagesOperation>? SrAddMessagesOps { get; set; }

        /// <summary>
        /// List of smart rollup cement operations, included in the block
        /// </summary>
        public IEnumerable<SmartRollupCementOperation>? SrCementOps { get; set; }

        /// <summary>
        /// List of smart rollup execute operations, included in the block
        /// </summary>
        public IEnumerable<SmartRollupExecuteOperation>? SrExecuteOps { get; set; }

        /// <summary>
        /// List of smart rollup originate operations, included in the block
        /// </summary>
        public IEnumerable<SmartRollupOriginateOperation>? SrOriginateOps { get; set; }

        /// <summary>
        /// List of smart rollup publish operations, included in the block
        /// </summary>
        public IEnumerable<SmartRollupPublishOperation>? SrPublishOps { get; set; }

        /// <summary>
        /// List of smart rollup recover bond operations, included in the block
        /// </summary>
        public IEnumerable<SmartRollupRecoverBondOperation>? SrRecoverBondOps { get; set; }

        /// <summary>
        /// List of smart rollup refute operations, included in the block
        /// </summary>
        public IEnumerable<SmartRollupRefuteOperation>? SrRefuteOps { get; set; }

        /// <summary>
        /// List of staking operations, included in the block
        /// </summary>
        public IEnumerable<StakingOperation>? StakingOps { get; set; }

        /// <summary>
        /// List of set delegate parameters operations, included in the block
        /// </summary>
        public IEnumerable<SetDelegateParametersOperation>? SetDelegateParametersOps { get; set; }

        /// <summary>
        /// List of DAL publish commitment operations, included in the block
        /// </summary>
        public IEnumerable<DalPublishCommitmentOperation>? DalPublishCommitmentOps { get; set; }

        /// <summary>
        /// List of migration operations, implicitly applied at the end of the block
        /// </summary>
        public IEnumerable<MigrationOperation>? Migrations { get; set; }

        /// <summary>
        /// List of revelation penalty operations, implicitly applied at the end of the block
        /// </summary>
        public IEnumerable<RevelationPenaltyOperation>? RevelationPenalties { get; set; }

        /// <summary>
        /// List of attestation rewards, implicitly applied at the end of the block
        /// </summary>
        public IEnumerable<AttestationRewardOperation>? AttestationRewards { get; set; }

        /// <summary>
        /// List of dal attestation rewards, implicitly applied at the end of the block
        /// </summary>
        public IEnumerable<DalAttestationRewardOperation>? DalAttestationRewards { get; set; }

        /// <summary>
        /// List of autostaking operations, implicitly applied at the end of the block
        /// </summary>
        public IEnumerable<AutostakingOperation>? AutostakingOps { get; set; }
        #endregion

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of block
        /// </summary>
        public QuoteShort? Quote { get; set; }
        #endregion

        #region [DEPRECATED]
        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public IEnumerable<UpdateSecondaryKeyOperation>? UpdateConsensusKeyOps => UpdateSecondaryKeyOps;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public IEnumerable<AttestationOperation>? Endorsements => Attestations;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public IEnumerable<PreattestationOperation>? Preendorsements => Preattestations;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public IEnumerable<DoubleConsensusOperation>? DoubleEndorsing => DoubleConsensus;

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public IEnumerable<DoubleConsensusOperation>? DoublePreendorsing => [];

        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public IEnumerable<AttestationRewardOperation>? EndorsingRewards => AttestationRewards;
        #endregion
    }
}
