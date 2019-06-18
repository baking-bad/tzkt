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
        [ForeignKey("TargetId")]
        public Contract Target { get; set; }
        #endregion
    }
}
