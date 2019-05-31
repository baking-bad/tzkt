using System.ComponentModel.DataAnnotations.Schema;
using Tezzycat.Data.Models.Base;

namespace Tezzycat.Data.Models
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
