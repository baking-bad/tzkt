namespace Tzkt.Api.Models
{
    public class BigMapKey
    {
        /// <summary>
        /// Internal Id, can be used for pagination
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Bigmap key status (`true` - active, `false` - removed)
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Key hash
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Key in JSON or Micheline format, depending on the `micheline` query parameter.
        /// </summary>
        public object Key { get; set; }

        /// <summary>
        /// Value in JSON or Micheline format, depending on the `micheline` query parameter.
        /// Note, if the key is inactive (removed) it will contain the last non-null value.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Level of the block where the bigmap key was seen first time
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Level of the block where the bigmap key was seen last time
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Total number of actions with the bigmap key
        /// </summary>
        public int Updates { get; set; }
    }
}
