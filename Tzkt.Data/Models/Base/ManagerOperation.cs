using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models.Base
{
    public class ManagerOperation : BaseOperation
    {
        public int SenderId { get; set; }
        public int Counter { get; set; }
        public long Fee { get; set; }

        public bool Applied { get; set; }
        public int? ParentId { get; set; }
        public int? Nonce { get; set; }

        #region relations
        [ForeignKey("SenderId")]
        public Contract Sender { get; set; }

        [ForeignKey("ParentId")]
        public TransactionOperation Parent { get; set; }
        #endregion
    }
}
