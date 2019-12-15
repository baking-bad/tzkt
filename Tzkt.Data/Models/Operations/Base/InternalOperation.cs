using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models.Base
{
    public class InternalOperation : ManagerOperation
    {
        public int? OriginalSenderId { get; set; }
        public int? Nonce { get; set; }

        #region relations
        [ForeignKey(nameof(OriginalSenderId))]
        public Account OriginalSender { get; set; }
        #endregion
    }
}
