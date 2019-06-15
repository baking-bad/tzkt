using System.ComponentModel.DataAnnotations.Schema;
using Tezzycat.Data.Models.Base;

namespace Tezzycat.Data.Models
{
    public class DelegationOperation : ManagerOperation
    {
        public int? DelegateId { get; set; }

        #region relations
        [ForeignKey("DelegateId")]
        public Contract Delegate { get; set; }
        #endregion
    }
}
