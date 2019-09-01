using System.ComponentModel.DataAnnotations.Schema;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class EndorsementOperation : BaseOperation
    {
        public int DelegateId { get; set; }
        public int Slots { get; set; }

        public long Reward { get; set; }

        #region relations
        [ForeignKey("DelegateId")]
        public Contract Delegate { get; set; }
        #endregion
    }
}
