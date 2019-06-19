using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models.Base
{
    public class BaseOperation
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public DateTime Timestamp { get; set; }
        public string OpHash { get; set; }

        #region relations
        [ForeignKey("Level")]
        public Block Block { get; set; }
        #endregion
    }
}
