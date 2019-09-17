using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models
{
    public class EndorsingRight
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int BakerId { get; set; }
        public int Slots { get; set; }

        #region relations
        [ForeignKey(nameof(BakerId))]
        public Delegate Baker { get; set; }
        #endregion
    }
}
