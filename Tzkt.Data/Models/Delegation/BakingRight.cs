using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models
{
    public class BakingRight
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int BakerId { get; set; }
        public int Priority { get; set; }

        #region relations
        [ForeignKey(nameof(BakerId))]
        public Delegate Baker { get; set; }
        #endregion
    }
}
