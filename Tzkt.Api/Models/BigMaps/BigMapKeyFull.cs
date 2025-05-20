namespace Tzkt.Api.Models
{
    public class BigMapKeyFull
    {
        /// <summary>
        /// Internal TzKT id.  
        /// **[sortable]**
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Bigmap ptr.
        /// </summary>
        public int Bigmap { get; set; }

        /// <summary>
        /// Key status (`true` - active, `false` - removed).
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Key hash.
        /// </summary>
        public required string Hash { get; set; }

        /// <summary>
        /// Key in JSON or Micheline format, depending on the `micheline` query parameter.  
        /// **[sortable]**
        /// </summary>
        public required object Key { get; set; }

        /// <summary>
        /// Value in JSON or Micheline format, depending on the `micheline` query parameter.
        /// Note, if the key is inactive (removed) this field will contain the last non-null value.  
        /// **[sortable]**
        /// </summary>
        public required object Value { get; set; }

        /// <summary>
        /// Level of the block where the key was first seen.  
        /// **[sortable]**
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the key was first seen.
        /// </summary>
        public DateTime FirstTime { get; set; }

        /// <summary>
        /// Level of the block where the key was last seen.  
        /// **[sortable]**
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the key was last seen.
        /// </summary>
        public DateTime LastTime { get; set; }

        /// <summary>
        /// Total number of actions with the key.  
        /// **[sortable]**
        /// </summary>
        public int Updates { get; set; }
    }
}
