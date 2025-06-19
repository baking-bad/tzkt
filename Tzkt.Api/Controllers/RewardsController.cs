using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/rewards")]
    public class RewardsController(RewardsRepository rewards, ResponseCacheService responseCache) : ControllerBase
    {

        /// <summary>
        /// Get baker cycle rewards count
        /// </summary>
        /// <remarks>
        /// Returns total number of cycles where the baker was active
        /// </remarks>
        /// <param name="address">Baker address</param>
        /// <returns></returns>
        [HttpGet("bakers/{address}/count")]
        public Task<int> GetBakerRewardsCount([Required][TzAddress] string address)
        {
            return rewards.GetBakerRewardsCount(address);
        }

        /// <summary>
        /// Get baker cycle rewards
        /// </summary>
        /// <remarks>
        /// Returns a list of baker rewards for every cycle, including future cycles.
        /// </remarks>
        /// <param name="address">Baker address.</param>
        /// <param name="cycle">Filters rewards by cycle.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts cycle rewards by specified field. Supported fields: `cycle` (default, desc).</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("bakers/{address}")]
        public async Task<ActionResult<IEnumerable<BakerRewards>>> GetBakerRewards(
            [Required][TzAddress] string address,
            Int32Parameter? cycle,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("cycle"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await rewards.GetBakerRewards(address, cycle, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await rewards.GetBakerRewards(address, cycle, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await rewards.GetBakerRewards(address, cycle, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields!.Length == 1)
                    return Ok(await rewards.GetBakerRewards(address, cycle, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await rewards.GetBakerRewards(address, cycle, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get delegator cycle rewards count
        /// </summary>
        /// <remarks>
        /// Returns total number of cycles where the delegator was delegated to an active baker
        /// </remarks>
        /// <param name="address">Delegator address</param>
        /// <returns></returns>
        [HttpGet("delegators/{address}/count")]
        public Task<int> GetDelegatorRewardsCount([Required][Address] string address)
        {
            return rewards.GetDelegatorRewardsCount(address);
        }

        /// <summary>
        /// Get delegator cycle rewards
        /// </summary>
        /// <remarks>
        /// Returns a list of delegator rewards for every cycle, including future cycles.
        /// </remarks>
        /// <param name="address">Delegator address.</param>
        /// <param name="cycle">Filters rewards by cycle.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts cycle rewards by specified field. Supported fields: `cycle` (default, desc).</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("delegators/{address}")]
        public async Task<ActionResult<IEnumerable<DelegatorRewards>>> GetDelegatorRewards(
            [Required][Address] string address,
            Int32Parameter? cycle,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("cycle"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await rewards.GetDelegatorRewards(address, cycle, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await rewards.GetDelegatorRewards(address, cycle, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await rewards.GetDelegatorRewards(address, cycle, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields!.Length == 1)
                    return Ok(await rewards.GetDelegatorRewards(address, cycle, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await rewards.GetDelegatorRewards(address, cycle, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get reward split
        /// </summary>
        /// <remarks>
        /// Returns baker rewards for the specified cycle with all delegator balances at that cycle to allow rewards distribution in proportion to shares.
        /// </remarks>
        /// <param name="baker">Baker address</param>
        /// <param name="cycle">Rewards cycle</param>
        /// <param name="offset">Specifies how many delegators in the reward split should be skipped</param>
        /// <param name="limit">Maximum number of delegators to return</param>
        /// <returns></returns>
        [HttpGet("split/{baker}/{cycle:int}")]
        public Task<RewardSplit?> GetRewardSplit([Required][TzAddress] string baker, [Min(0)] int cycle, int offset = 0, [Range(0, 10000)] int limit = 100)
        {
            return rewards.GetRewardSplit(baker, cycle, offset, limit);
        }

        /// <summary>
        /// Get reward split delegator
        /// </summary>
        /// <remarks>
        /// Returns delegator/staker from the reward split for the specified cycle.
        /// </remarks>
        /// <param name="baker">Baker address</param>
        /// <param name="cycle">Reward split cycle</param>
        /// <param name="delegator">Delegator address</param>
        /// <returns></returns>
        [HttpGet("split/{baker}/{cycle:int}/{delegator}")]
        public Task<SplitDelegator?> GetRewardSplitDelegator([Required][TzAddress] string baker, [Min(0)] int cycle, [Required][Address] string delegator)
        {
            return rewards.GetRewardSplitDelegator(baker, cycle, delegator);
        }

        /// <summary>
        /// Get staker rewards
        /// </summary>
        /// <remarks>
        /// Returns a list of staker rewards.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("stakers")]
        public async Task<ActionResult<IEnumerable<DelegatorRewards>>> GetStakerRewards(
            [FromQuery] StakerRewardsFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection), ("quote", quote));

            if (responseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (selection.select == null)
            {
                res = await rewards.GetStakerRewards(filter, pagination, quote);
            }
            else
            {
                res = new SelectionResponse
                {
                    Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                    Rows = await rewards.GetStakerRewards(filter, pagination, selection, quote)
                };
            }
            cached = responseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get staker rewards count
        /// </summary>
        /// <remarks>
        /// Returns total number of staker rewards.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("stakers/count")]
        public async Task<ActionResult<int>> GetStakerRewardsCount([FromQuery] StakerRewardsFilter filter)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (responseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await rewards.GetStakerRewardsCount(filter);
            cached = responseCache.Set(query, res);
            return this.Bytes(cached);
        }
    }
}
