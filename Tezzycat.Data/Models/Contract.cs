using System.ComponentModel.DataAnnotations.Schema;

namespace Tezzycat.Data.Models
{
    public class Contract
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public string PublicKey { get; set; }
        public int DelegateId { get; set; }
        public int ManagerId { get; set; }

        public bool Code { get; set; }
        public bool Delegatable { get; set; }
        public bool Spendable { get; set; }

        #region state
        public bool Active { get; set; }
        public long Balance { get; set; }
        public long Counter { get; set; }
        #endregion

        #region relations
        [ForeignKey("DelegateId")]
        public Contract Delegate { get; set; }

        [ForeignKey("ManagerId")]
        public Contract Manager { get; set; }
        #endregion
    }
}
