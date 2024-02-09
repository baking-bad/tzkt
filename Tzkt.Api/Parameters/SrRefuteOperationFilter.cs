using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class SrRefuteOperationFilter : SrOperationFilter
    {
        /// <summary>
        /// Filter by any of the specified fields (`sender`, `initiator`, or `opponent`).
        /// Example: `anyof.initiator.opponent=mv1...` will return operations where `initiator` OR `opponent` is equal to the specified value.
        /// This parameter is useful when you need to get all operations somehow related to the account in a single request.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AnyOfParameter anyof { get; set; }

        /// <summary>
        /// Filter by game info.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public SrGameInfoFilter game { get; set; }

        /// <summary>
        /// Filter by refutation game move (`start`, `dissection`, `proof`, or `timeout`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public RefutationMoveParameter move { get; set; }

        /// <summary>
        /// Filter by refutation game status (`none`, `ongoing`, `loser`, or `draw`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public RefutationGameStatusParameter gameStatus { get; set; }

        public override bool Empty =>
            base.Empty &&
            anyof == null &&
            game.Empty &&
            move == null &&
            gameStatus == null;

        public override string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("hash", hash), ("counter", counter), ("level", level),
                ("timestamp", timestamp), ("status", status), ("sender", sender),
                ("rollup", rollup), ("anyof", anyof), ("game", game), ("move", move),
                ("gameStatus", gameStatus));
        }
    }
}
