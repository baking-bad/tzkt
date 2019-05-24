using System.ComponentModel.DataAnnotations.Schema;

namespace Tezzycat.Models.Base
{
    public class ManagerOperation : BaseOperation
    {
        public int SenderId { get; set; }
        public int Counter { get; set; }
        public long Fee { get; set; }

        public bool Applied { get; set; }
        public bool Internal { get; set; }

        #region relations
        [ForeignKey("SenderId")]
        public Contract Sender { get; set; }
        #endregion
    }
}
