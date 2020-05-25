using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class DelegateInfo
    {
        /// <summary>
        /// Name of the baking service
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Public key hash of the delegate (baker)
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Delegation status (`true` - active, `false` - deactivated)
        /// </summary>
        public bool Active { get; set; }
    }
}
