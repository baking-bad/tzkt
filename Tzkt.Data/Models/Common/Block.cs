using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models
{
    public class Block
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public string Hash { get; set; }
        public DateTime Timestamp { get; set; }
        public int ProtocolId { get; set; }

        public int? BakerId { get; set; }
        public int Priority { get; set; }
        public int Operations { get; set; }
        public int OperationsMask { get; set; }
        public int Validations { get; set; }
        public int? RevelationId { get; set; }

        #region relations
        [ForeignKey(nameof(ProtocolId))]
        public Protocol Protocol { get; set; }

        [ForeignKey(nameof(BakerId))]
        public Delegate Baker { get; set; }

        [ForeignKey(nameof(RevelationId))]
        public NonceRevelationOperation Revelation { get; set; }

        public List<Delegate> ActivatedDelegates { get; set; }
        public List<Delegate> DeactivatedDelegates { get; set; }

        #region operations
        public List<EndorsementOperation> Endorsements { get; set; }

        public List<BallotOperation> Ballots { get; set; }
        public List<ProposalOperation> Proposals { get; set; }

        public List<ActivationOperation> Activations { get; set; }
        public List<DoubleBakingOperation> DoubleBakings { get; set; }
        public List<DoubleEndorsingOperation> DoubleEndorsings { get; set; }
        public List<NonceRevelationOperation> Revelations { get; set; }

        public List<DelegationOperation> Delegations { get; set; }
        public List<OriginationOperation> Originations { get; set; }
        public List<TransactionOperation> Transactions { get; set; }
        public List<RevealOperation> Reveals { get; set; }
        #endregion
        #endregion
    }
}
