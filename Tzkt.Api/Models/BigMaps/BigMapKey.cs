namespace Tzkt.Api.Models
{
    public class BigMapKey
    {
        /// <summary>
        /// Bigmap key status: `true` - active, `false` - removed
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
