using System.Collections.Generic;

namespace Tzkt.Data.Models
{
    public class User : Account
    {
        public string PublicKey { get; set; }
        public long Counter { get; set; }

        #region relations
        public List<Contract> ManagedContracts { get; set; }

        #region operations
        public ActivationOperation Activation { get; set; }
        public List<OriginationOperation> ManagedOriginations { get; set; }
        #endregion
        #endregion
    }
}
