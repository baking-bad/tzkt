using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tezzycat.Data.Models
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

        #region relations
        [ForeignKey("DelegateId")]
        public Contract Delegate { get; set; }
        public List<Contract> DelegatedContracts { get; set; }

        [ForeignKey("ManagerId")]
        public Contract Manager { get; set; }
        public List<Contract> OriginatedContracts { get; set; }
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
