using System.ComponentModel.DataAnnotations.Schema;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class ActivationOperation : BaseOperation
    {
        public int AccountId { get; set; }
        public long Balance { get; set; }

        #region relations
        [ForeignKey("AccountId")]
        public Contract Account { get; set; }
        #endregion
    }
}
