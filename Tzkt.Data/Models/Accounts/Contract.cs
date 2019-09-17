using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models
{
    public class Contract : Account
    {
        public int? ManagerId { get; set; }
        public int? OriginatorId { get; set; }

        public bool Delegatable { get; set; }
        public bool Spendable { get; set; }

        #region relations
        [ForeignKey(nameof(ManagerId))]
        public User Manager { get; set; }

        [ForeignKey(nameof(OriginatorId))]
        public Account Originator { get; set; }

        #region operations
        public OriginationOperation Origination { get; set; }
        #endregion
        #endregion
    }
}
