using System.ComponentModel.DataAnnotations.Schema;
using Tezzycat.Data.Models.Base;

namespace Tezzycat.Data.Models
{
    public class DoubleBakingOperation : AnonimousOperation
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
