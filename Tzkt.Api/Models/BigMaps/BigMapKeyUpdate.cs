using System;

namespace Tzkt.Api.Models
{
    public class BigMapKeyUpdate
    {
        /// <summary>
        /// Internal Id, can be used for pagination
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Level of the block where the bigmap key was updated
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Timestamp of the block where the bigmap key was updated
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Action with the key (`add_key`, `update_key`, `remove_key`)
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Value in JSON or Micheline format, depending on the `micheline` query parameter.
        /// Note, if the action is `remove_key` it will contain the last non-null value.
        /// </summary>
        public object Value { get; set; }
    }
}
