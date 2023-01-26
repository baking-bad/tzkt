using System;

namespace Tzkt.Api.Models
{
    public class Domain
    {
        /// <summary>
        /// Internal TzKT id.  
        /// **[sortable]**
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Domain level (i.e. tez has level=1, domain.tez has level=2, subdomain.domain.tez has level=3, etc.).
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Domain record name.  
        /// **[sortable]**
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Owner of the domain.
        /// </summary>
        public Alias Owner { get; set; }

        /// <summary>
        /// Address the domain points to.
        /// </summary>
        public Alias Address { get; set; }

        /// <summary>
        /// Whether or not the domain is on the reverse records list
        /// </summary>
        public bool Reverse { get; set; }

        /// <summary>
        /// Expiration datetime
        /// </summary>
        public DateTime Expiration { get; set; }

        /// <summary>
        /// Arbitrary data bound to the domain.
        /// </summary>
        public RawJson Data { get; set; }

        /// <summary>
        /// Level of the block where the domain was first seen.  
        /// **[sortable]**
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the domain was first seen.
        /// </summary>
        public DateTime FirstTime { get; set; }

        /// <summary>
        /// Level of the block where the domain was last seen.  
        /// **[sortable]**
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the domain was last seen.
        /// </summary>
        public DateTime LastTime { get; set; }
    }
}
