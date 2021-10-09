using System;
using System.Collections.Generic;

namespace Tzkt.Api.Models
{
    public class Delegate : Account
    {
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
        /// Amount of security deposit, currently locked for baked (produced) blocks and (or) given endorsements (micro tez)
        /// </summary>
        public long FrozenDeposits { get; set; }

        /// <summary>
        /// Amount of currently frozen baking rewards (micro tez)
        /// </summary>
        public long FrozenRewards { get; set; }

        /// <summary>
        /// Amount of currently frozen fees paid by operations inside blocks, baked (produced) by the delegate (micro tez)
        /// </summary>
        public long FrozenFees { get; set; }
        
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
        /// Sum of delegate (baker) balance and delegated funds minus frozen rewards (micro tez)
        /// </summary>
        public long StakingBalance { get; set; }

        /// <summary>
        /// Number of contracts, created (originated) and/or managed by the delegate (baker)
        /// </summary>
        public int NumContracts { get; set; }

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
        /// Number of seed nonce revelation (are used by the blockchain to create randomness) operations provided by the delegate
        /// </summary>
        public int NumNonceRevelations { get; set; }

        /// <summary>
        /// Number of operations for all time in which rewards were lost due to unrevealed seed nonces by the delegate (synthetic type)
        /// </summary>
        public int NumRevelationPenalties { get; set; }

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
        /// Number of register global constant operations of the delegate (baker)
        /// </summary>
        public int NumRegisterConstants { get; set; }
        
        /// <summary>
        /// Number of migration (result of the context (database) migration during a protocol update) operations,
        /// related to the delegate (synthetic type) 
        /// </summary>
        public int NumMigrations { get; set; }

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
        public AccountMetadata Metadata { get; set; }

        /// <summary>
        /// Last seen baker's software
        /// </summary>
        public SoftwareAlias Software { get; set; }
    }
}
