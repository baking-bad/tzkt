using System.ComponentModel.DataAnnotations.Schema;

namespace Tezzycat.Models
{
    public class EndorsingRight
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int BakerId { get; set; }
        public int Slots { get; set; }

        #region relations
        [ForeignKey("BakerId")]
        public Contract Baker { get; set; }
        #endregion
    }
}
