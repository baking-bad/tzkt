using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models.Base
{
    public class ManagerOperation : BaseOperation
    {
        public int SenderId { get; set; }
        public int Counter { get; set; }
        public long Fee { get; set; }

        public OperationStatus Status { get; set; }
        public int? ParentId { get; set; }
        public int? Nonce { get; set; }

        #region relations
        [ForeignKey(nameof(SenderId))]
        public Account Sender { get; set; }

        [ForeignKey(nameof(ParentId))]
        public TransactionOperation Parent { get; set; }
        #endregion
    }

    public enum OperationStatus : byte
    {
        None,
        Applied,
        Backtracked,
        Skipped,
        Failed
    }
}
