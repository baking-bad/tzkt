using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/smart_rollups")]
    public class SmartRollupsController : ControllerBase
    {
        readonly SmartRollupsRepository SmartRollups;
        readonly StateCache State;
        readonly ResponseCacheService ResponseCache;

        public SmartRollupsController(SmartRollupsRepository srCommitments, StateCache state, ResponseCacheService responseCache)
        {
            SmartRollups = srCommitments;
            State = state;
            ResponseCache = responseCache;
        }

        #region commitments
        /// <summary>
        /// Get smart rollup commitments
        /// </summary>
        /// <remarks>
        /// Returns a total number of smart rollup commitments.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("commitments/count")]
        public async Task<ActionResult<int>> GetSmartRollupCommitmentsCount([FromQuery] SrCommitmentFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.SmartRollupCommitmentCounter);

            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, await SmartRollups.GetCommitmentsCount(filter));
            
            return this.Bytes(res);
        }

        /// <summary>
        /// Get smart rollup commitments
        /// </summary>
        /// <remarks>
        /// Returns a list of smart rollup commitments.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <returns></returns>
        [HttpGet("commitments")]
        public async Task<ActionResult<IEnumerable<SrCommitment>>> GetSmartRollupCommitments(
            [FromQuery] SrCommitmentFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value, 
                ("filter", filter), ("pagination", pagination), ("selection", selection));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, selection.select == null
                    ? await SmartRollups.GetCommitments(filter, pagination)
                    : new SelectionResponse
                    {
                        Cols = selection.Cols,
                        Rows = await SmartRollups.GetCommitments(filter, pagination, selection)
                    });
            
            return this.Bytes(res);
        }
        #endregion

        #region games
        /// <summary>
        /// Get smart rollup games
        /// </summary>
        /// <remarks>
        /// Returns a total number of smart rollup refutation games.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("games/count")]
        public async Task<ActionResult<int>> GetSmartRollupGamesCount([FromQuery] SrGameFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.RefutationGameCounter);

            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, await SmartRollups.GetGamesCount(filter));

            return this.Bytes(res);
        }

        /// <summary>
        /// Get smart rollup games
        /// </summary>
        /// <remarks>
        /// Returns a list of smart rollup refutation games.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <returns></returns>
        [HttpGet("games")]
        public async Task<ActionResult<IEnumerable<SrGame>>> GetSmartRollupGames(
            [FromQuery] SrGameFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection));

            //if (!ResponseCache.TryGet(query, out var res))
                var res = ResponseCache.Set(query, selection.select == null
                    ? await SmartRollups.GetGames(filter, pagination)
                    : new SelectionResponse
                    {
                        Cols = selection.Cols,
                        Rows = await SmartRollups.GetGames(filter, pagination, selection)
                    });

            return this.Bytes(res);
        }
        #endregion
    }
}
