using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models.Base
{
    public class InternalOperation : ManagerOperation
    {
        public int? ParentId { get; set; }
        public int? Nonce { get; set; }

        #region relations
        [ForeignKey(nameof(ParentId))]
        public TransactionOperation Parent { get; set; }
        #endregion
    }
}
