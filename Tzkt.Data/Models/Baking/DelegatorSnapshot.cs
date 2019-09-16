using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models
{
    public class DelegatorSnapshot
    {
        public int Id { get; set; }
        public int Cycle { get; set; }
        public int DelegatorId { get; set; }

        public int BakerId { get; set; }
        public long Balance { get; set; }

        #region relations
        [ForeignKey(nameof(DelegatorId))]
        public BaseAddress Delegator { get; set; }

        [ForeignKey(nameof(BakerId))]
        public Delegate Baker { get; set; }
        #endregion
    }
}
