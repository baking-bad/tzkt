using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models
{
    public class DelegatorStat
    {
        public int Id { get; set; }
        public int Cycle { get; set; }
        public int DelegatorId { get; set; }

        public int BakerId { get; set; }
        public long Balance { get; set; }

        #region relations
        [ForeignKey("DelegatorId")]
        public Contract Delegator { get; set; }

        [ForeignKey("BakerId")]
        public Contract Baker { get; set; }
        #endregion
    }
}
