namespace Tzkt.Api.Models
{
    public class BigMapDiff
    {
        /// <summary>
        /// Bigmap Id
        /// </summary>
        public int Bigmap { get; set; }

        /// <summary>
        /// Path to the bigmap in the contract storage
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Action with the bigmap (`allocate`, `add_key`, `update_key`, `remove_key`, `remove`)
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Affected key.
        /// If the action is `allocate` or `remove` the key will be `null`.
        /// </summary>
        public BigMapDiffKey Key { get; set; }
    }

    public class BigMapDiffKey
    {
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
        /// Note, if the action is `remove_key` it will contain the last non-null value.
        /// </summary>
        public object Value { get; set; }
    }
}
