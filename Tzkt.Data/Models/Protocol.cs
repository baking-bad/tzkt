using System.Collections.Generic;

namespace Tzkt.Data.Models
{
    public class Protocol
    {
        public int Id { get; set; }
        public int Weight { get; set; }
        public string Hash { get; set; }

        #region relations
        public List<Block> Blocks { get; set; }
        #endregion
    }
}
