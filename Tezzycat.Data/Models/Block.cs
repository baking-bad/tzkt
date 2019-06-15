using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tezzycat.Data.Models
{
    public class Block
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public string Hash { get; set; }
        public DateTime Timestamp { get; set; }
        public int ProtocolId { get; set; }

        public int? BakerId { get; set; }
        public int Priority { get; set; }
        public int Validations { get; set; }

        #region relations
        [ForeignKey("ProtocolId")]
        public Protocol Protocol { get; set; }

        [ForeignKey("BakerId")]
        public Contract Baker { get; set; }
        #endregion
    }
}
