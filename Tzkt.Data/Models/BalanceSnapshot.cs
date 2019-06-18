using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models
{
    public class BalanceSnapshot
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int ContractId { get; set; }
        public int DelegateId { get; set; }
        public long Balance { get; set; }

        #region relations
        [ForeignKey("ContractId")]
        public Contract Contract { get; set; }

        [ForeignKey("DelegateId")]
        public Contract Delegate { get; set; }
        #endregion
    }
}
