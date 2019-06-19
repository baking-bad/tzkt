using System.ComponentModel.DataAnnotations.Schema;

using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class NonceRevelationOperation : BaseOperation
    {
        public int BakerId { get; set; }
        public int RevelationLevel { get; set; }

        #region relations
        [ForeignKey("BakerId")]
        public Contract Baker { get; set; }

        public Block RevelationBlock { get; set; }
        #endregion
    }
}
