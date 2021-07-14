using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class Block
    {
        /// <summary>
        /// The height of the block from the genesis block
        /// </summary>
        public int Level { get; set; }
        
        /// <summary>
        /// Block hash
        /// </summary>
        public string Hash { get; set; }
        
        /// <summary>
        /// The datetime at which the block is claimed to have been created (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Protocol code, representing a number of protocol changes since genesis (mod 256, but `-1` for the genesis block)
        /// </summary>
        public int Proto { get; set; }
        
        /// <summary>
        /// The position in the priority list of delegates at which the block was baked
        /// </summary>
        public int Priority { get; set; }
        
        /// <summary>
        /// Number of endorsements, confirmed the block
        /// </summary>
        public int Validations { get; set; }

        /// <summary>
        /// Security deposit frozen on the baker's account for producing the block (micro tez)
        /// </summary>
        public long Deposit { get; set; }

        /// <summary>
        /// Reward of the baker for producing the block (micro tez)
        /// </summary>
        public long Reward { get; set; }

        /// <summary>
        /// Total fee paid by all operations, included in the block
        /// </summary>
        public long Fees { get; set; }

        /// <summary>
        /// Status of the seed nonce revelation
        /// `true` - seed nonce revealed
        /// `false` - there's no `seed_nonce_hash` in the block or seed nonce revelation has missed
        /// </summary>
        public bool NonceRevealed { get; set; }

        /// <summary>
        /// Information about a delegate (baker), produced the block
        /// </summary>
        public Alias Baker { get; set; }

        /// <summary>
        /// Information about baker's software
        /// </summary>
        public SoftwareAlias Software { get; set; }

        /// <summary>
        /// Flag indicating that the baker has voted for disabling liquidity baking
        /// </summary>
        public bool LBEscapeVote { get; set; }

        /// <summary>
        /// Liquidity baking escape EMA value with precision of 1000 for integer computation
        /// </summary>
        public int LBEscapeEma { get; set; }

        #region operations
        /// <summary>
        /// List of endorsement (is operation, which specifies the head of the chain as seen by the endorser of a given slot)
        /// operations, included in the block
        /// </summary>
        public IEnumerable<EndorsementOperation> Endorsements { get; set; }

        /// <summary>
        /// List of proposal (is used by bakers (delegates) to submit and/or upvote proposals to amend the protocol)
        /// operations, included in the block
        /// </summary>
        public IEnumerable<ProposalOperation> Proposals { get; set; }
        
        /// <summary>
        /// List of ballot (is used to vote for a proposal in a given voting cycle) operations, included in the block
        /// </summary>
        public IEnumerable<BallotOperation> Ballots { get; set; }

        /// <summary>
        /// List of activation (is used to activate accounts that were recommended allocations of
        /// tezos tokens for donations to the Tezos Foundation’s fundraiser) operations, included in the block
        /// </summary>
        public IEnumerable<ActivationOperation> Activations { get; set; }
        
        /// <summary>
        /// List of double baking evidence (is used by bakers to provide evidence of double baking (baking two different
        /// blocks at the same height) by a baker) operations, included in the block
        /// </summary>
        public IEnumerable<DoubleBakingOperation> DoubleBaking { get; set; }
        
        /// <summary>
        /// List of double endorsement evidence (is used by bakers to provide evidence of double endorsement
        /// (endorsing two different blocks at the same block height) by a baker) operations, included in the block
        /// </summary>
        public IEnumerable<DoubleEndorsingOperation> DoubleEndorsing { get; set; }
        
        /// <summary>
        /// List of nonce revelation (are used by the blockchain to create randomness) operations, included in the block
        /// </summary>
        public IEnumerable<NonceRevelationOperation> NonceRevelations { get; set; }

        /// <summary>
        /// List of delegation (is used to delegate funds to a delegate (an implicit account registered as a baker))
        /// operations, included in the block
        /// </summary>
        public IEnumerable<DelegationOperation> Delegations { get; set; }
        
        /// <summary>
        /// List of origination (deployment / contract creation ) operations, included in the block
        /// </summary>
        public IEnumerable<OriginationOperation> Originations { get; set; }
        
        /// <summary>
        /// List of transaction (is a standard operation used to transfer tezos tokens to an account)
        /// operations, included in the block
        /// </summary>
        public IEnumerable<TransactionOperation> Transactions { get; set; }
        
        /// <summary>
        /// List of reveal (is used to reveal the public key associated with an account) operations, included in the block
        /// </summary>
        public IEnumerable<RevealOperation> Reveals { get; set; }
        #endregion

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of block
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion
    }
}
