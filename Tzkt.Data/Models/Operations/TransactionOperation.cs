using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class TransactionOperation : ManagerOperation
    {
        public int TargetId { get; set; }
        public bool TargetAllocated { get; set; }

        public long Amount { get; set; }
        public long StorageFee { get; set; }

        #region relations
        [ForeignKey(nameof(TargetId))]
        public BaseAddress Target { get; set; }

        public List<DelegationOperation> InternalDelegations { get; set; }
        public List<OriginationOperation> InternalOriginations { get; set; }
        public List<TransactionOperation> InternalTransactions { get; set; }
        public List<RevealOperation> InternalReveals { get; set; }
        #endregion
    }
}
