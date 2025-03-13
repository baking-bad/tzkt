using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/staking")]
    public class StakingController : ControllerBase
    {
        readonly StakingRepository Repo;
        readonly StateCache State;
        readonly ResponseCacheService ResponseCache;

        public StakingController(StakingRepository repo, StateCache state, ResponseCacheService responseCache)
        {
            Repo = repo;
            State = state;
            ResponseCache = responseCache;
        }

        #region staking updates
        /// <summary>
        /// Get staking updates count
        /// </summary>
        /// <remarks>
        /// Returns a total number of staking updates.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("updates/count")]
        public async Task<ActionResult<int>> GetStakingUpdatesCount([FromQuery] StakingUpdateFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.StakingUpdatesCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Repo.GetStakingUpdatesCount(filter);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get staking updates
        /// </summary>
        /// <remarks>
        /// Returns a list of staking updates.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <returns></returns>
        [HttpGet("updates")]
        public async Task<ActionResult<IEnumerable<StakingUpdate>>> GetStakingUpdates(
            [FromQuery] StakingUpdateFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (selection.select == null)
            {
                res = await Repo.GetStakingUpdates(filter, pagination);
            }
            else
            {
                res = new SelectionResponse
                {
                    Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                    Rows = await Repo.GetStakingUpdates(filter, pagination, selection)
                };
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region unstake requests
        /// <summary>
        /// Get unstake requests count
        /// </summary>
        /// <remarks>
        /// Returns a total number of unstake requests.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("unstake_requests/count")]
        public async Task<ActionResult<int>> GetUnstakeRequestsCount([FromQuery] UnstakeRequestFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.UnstakeRequestsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Repo.GetUnstakeRequestsCount(filter);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get unstake requests
        /// </summary>
        /// <remarks>
        /// Returns a list of unstake requests.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <returns></returns>
        [HttpGet("unstake_requests")]
        public async Task<ActionResult<IEnumerable<UnstakeRequest>>> GetUnstakeRequests(
            [FromQuery] UnstakeRequestFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (selection.select == null)
            {
                res = await Repo.GetUnstakeRequests(filter, pagination);
            }
            else
            {
                res = new SelectionResponse
                {
                    Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                    Rows = await Repo.GetUnstakeRequests(filter, pagination, selection)
                };
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion
    }
}
