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
        readonly SrCommitmentsRepository SrCommitments;
        readonly StateCache State;
        readonly ResponseCacheService ResponseCache;

        public SmartRollupsController(SrCommitmentsRepository srCommitments, StateCache state, ResponseCacheService responseCache)
        {
            SrCommitments = srCommitments;
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
                res = ResponseCache.Set(query, await SrCommitments.GetCount(filter));
            
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
                    ? await SrCommitments.Get(filter, pagination)
                    : new SelectionResponse
                    {
                        Cols = selection.Cols,
                        Rows = await SrCommitments.Get(filter, pagination, selection)
                    });
            
            return this.Bytes(res);
        }
        #endregion
    }
}
