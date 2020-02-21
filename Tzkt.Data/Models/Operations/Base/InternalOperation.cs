using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models.Base
{
    public class InternalOperation : ManagerOperation
    {
        public int? InitiatorId { get; set; }
        public int? Nonce { get; set; }

        #region relations
        [ForeignKey(nameof(InitiatorId))]
        public Account Initiator { get; set; }
        #endregion
    }
}
