namespace Tzkt.Api.Models
{
    public class BigMapKeyShort
    {
        /// <summary>
        /// Internal Id, can be used for pagination
        /// </summary>
        public int Id { get; set; }

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
    }
}
