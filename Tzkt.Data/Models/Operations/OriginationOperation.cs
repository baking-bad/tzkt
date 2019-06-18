using System.ComponentModel.DataAnnotations.Schema;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class OriginationOperation : ManagerOperation
    {
        public int ContractId { get; set; }
        public int? DelegateId { get; set; }
        public int ManagerId { get; set; }
        public bool Delegatable { get; set; }
        public bool Spendable { get; set; }
        public long Balance { get; set; }
        public long StorageFee { get; set; }

        #region relations
        [ForeignKey("ContractId")]
        public Contract Contract { get; set; }

        [ForeignKey("DelegateId")]
        public Contract Delegate { get; set; }

        [ForeignKey("ManagerId")]
        public Contract Manager { get; set; }
        #endregion
    }
}
