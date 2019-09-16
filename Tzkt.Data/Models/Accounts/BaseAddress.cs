using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models
{
    public abstract class BaseAddress
    {
        public int Id { get; set; }
        public AddressType Type { get; set; }
        public string Address { get; set; }

        public int? DelegateId { get; set; }
        public bool Staked { get; set; }

        public long Balance { get; set; }
        public Operations Operations { get; set; }

        #region relations
        [ForeignKey(nameof(DelegateId))]
        public Delegate Delegate { get; set; }

        public List<Contract> OriginatedContracts { get; set; }
        public List<DelegatorSnapshot> BalanceSnapshots { get; set; }

        #region operations
        public List<DelegationOperation> SentDelegations { get; set; }
        public List<OriginationOperation> SentOriginations { get; set; }
        public List<TransactionOperation> SentTransactions { get; set; }
        public List<TransactionOperation> ReceivedTransactions { get; set; }
        public List<RevealOperation> Reveals { get; set; }
        #endregion
        #endregion
    }

    public enum AddressType
    {
        Account,
        Delegate,
        Contract
    }
}
