using System.ComponentModel.DataAnnotations.Schema;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class DoubleBakingOperation : BaseOperation
    {
        public int AccusedLevel { get; set; }

        public int AccuserId { get; set; }
        public long Reward { get; set; }

        public int OffenderId { get; set; }
        public long Burned { get; set; }

        #region relations
        [ForeignKey("AccuserId")]
        public Contract Accuser { get; set; }

        [ForeignKey("OffenderId")]
        public Contract Offender { get; set; }
        #endregion
    }
}
