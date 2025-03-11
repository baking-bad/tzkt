namespace Tzkt.Api.Models
{
    public class BigMapKeyShort
    {
        /// <summary>
        /// Key hash
        /// </summary>
        public required string Hash { get; set; }

        /// <summary>
        /// Key in JSON or Micheline format, depending on the `micheline` query parameter.
        /// </summary>
        public required object Key { get; set; }

        /// <summary>
        /// Value in JSON or Micheline format, depending on the `micheline` query parameter.
        /// Note, if the action is `remove_key` it will contain the last non-null value.
        /// </summary>
        public required object Value { get; set; }
    }
}
