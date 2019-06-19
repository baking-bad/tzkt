using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models
{
    public class Contract
    {
        public int Id { get; set; }
        public ContractKind Kind { get; set; }
        public string Address { get; set; }
        public string PublicKey { get; set; }
        public int? DelegateId { get; set; }
        public int? ManagerId { get; set; }

        public bool Delegatable { get; set; }
        public bool Spendable { get; set; }
        public bool Staked { get; set; }

        public long Balance { get; set; }
        public long Counter { get; set; }
        public long Frozen { get; set; }

        public long StakingBalance { get; set; }
        public int DelegatorsCount { get; set; }

        public int Operations { get; set; }
        public int OperationsMask { get; set; }

        #region relations
        [ForeignKey("DelegateId")]
        public Contract Delegate { get; set; }

        [ForeignKey("ManagerId")]
        public Contract Manager { get; set; }

        public List<Block> BakedBlocks { get; set; }
        public List<Contract> DelegatedContracts { get; set; }
        public List<Contract> OriginatedContracts { get; set; }

        #region operations
        public List<EndorsementOperation> Endorsements { get; set; }

        public List<BallotOperation> Ballots { get; set; }
        public List<ProposalOperation> Proposals { get; set; }

        public ActivationOperation Activation { get; set; }
        public List<DoubleBakingOperation> DoubleBakings { get; set; }
        public List<DoubleBakingOperation> DoubleBakingAccusations { get; set; }
        public List<DoubleEndorsingOperation> DoubleEndorsings { get; set; }
        public List<DoubleEndorsingOperation> DoubleEndorsingAccusations { get; set; }
        public List<NonceRevelationOperation> Revelations { get; set; }

        public OriginationOperation Origination { get; set; }
        public List<DelegationOperation> Delegations { get; set; }
        public List<DelegationOperation> IncomingDelegations { get; set; }
        public List<OriginationOperation> Originations { get; set; }
        public List<OriginationOperation> ManagedOriginations { get; set; }
        public List<OriginationOperation> DelegatedOriginations { get; set; }
        public List<TransactionOperation> IncomingTransactions { get; set; }
        public List<TransactionOperation> OutgoingTransactions { get; set; }
        public List<RevealOperation> Reveals { get; set; }
        #endregion
        #endregion
    }

    public enum ContractKind
    {
        Account,
        Baker,
        Originated,
        SmartContract
    }
}
