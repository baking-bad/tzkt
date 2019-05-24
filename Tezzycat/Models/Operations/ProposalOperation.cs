using System.ComponentModel.DataAnnotations.Schema;
using Tezzycat.Models.Base;

namespace Tezzycat.Models
{
    public class ProposalOperation : BaseOperation
    {
        public int Period { get; set; }
        public int ProposalId { get; set; }
        public int SenderId { get; set; }

        #region relations
        [ForeignKey("ProposalId")]
        public Proposal Proposal { get; set; }

        [ForeignKey("SenderId")]
        public Contract Sender { get; set; }
        #endregion
    }
}
