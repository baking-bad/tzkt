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
        public required string Path { get; set; }

        /// <summary>
        /// Action with the bigmap (`allocate`, `add_key`, `update_key`, `remove_key`, `remove`)
        /// </summary>
        public required string Action { get; set; }

        /// <summary>
        /// Affected key.
        /// If the action is `allocate` or `remove` the content will be `null`.
        /// </summary>
        public BigMapKeyShort? Content { get; set; }
    }
}
