namespace Mvkt.Api.Models
{
    public class SrGameMove
    {
        /// <summary>
        /// Unique ID of the operation, stored in the MvKT indexer database.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The height of the block from the genesis block, in which the operation was included.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Datetime of the block, in which the operation was included (ISO 8601, e.g. `2020-02-20T02:40:57Z`).
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Information about the account who has sent the operation.
        /// </summary>
        public Alias Sender { get; set; }

        /// <summary>
        /// Player's move (`start`, `dissection`, `proof`, `timeout`).
        /// </summary>
        public string Move { get; set; }

        /// <summary>
        /// Game status after the move
        /// (`ongoing` - game in progress, `loser` - one of the players lost, `draw` - both players lost).
        /// </summary>
        public string GameStatus { get; set; }
    }
}
