using System.ComponentModel.DataAnnotations.Schema;
using Tezzycat.Data.Models.Base;

namespace Tezzycat.Data.Models
{
    public class EndorsementOperation : BaseOperation
    {
        public int DelegateId { get; set; }
        public int SlotsCount { get; set; }
        public long Reward { get; set; }

        #region relations
        [ForeignKey("DelegateId")]
        public Contract Delegate { get; set; }
        #endregion
    }
}
