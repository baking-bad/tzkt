﻿using System.ComponentModel.DataAnnotations.Schema;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class BallotOperation : BaseOperation
    {
        public int Period { get; set; }
        public int ProposalId { get; set; }
        public int SenderId { get; set; }

        public Vote Vote { get; set; }

        #region relations
        [ForeignKey("ProposalId")]
        public Proposal Proposal { get; set; }

        [ForeignKey("SenderId")]
        public Contract Sender { get; set; }
        #endregion
    }

    public enum Vote
    {
        Yay,
        Nay,
        Pass
    }
}
