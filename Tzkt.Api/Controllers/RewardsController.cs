using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/rewards")]
    public class RewardsController(RewardsRepository rewards, AccountsCache accounts, ResponseCacheService responseCache) : ControllerBase
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
        public async Task<int> GetBakerRewardsCount([Required][TzAddress] string address)
        {
            if (await accounts.GetAsync(address) is not RawDelegate baker)
                return 0;

            return await rewards.GetBakerRewardsCount(new() { baker = baker });
        }

        /// <summary>
        /// Get baker cycle rewards
        /// </summary>
        /// <remarks>
        /// Returns a list of baker rewards for every cycle, including future cycles.
        /// </remarks>
        /// <param name="address">Baker address.</param>
        /// <param name="cycle">Filter by cycle.</param>
        /// <param name="pagination"></param>
        /// <param name="selection"></param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("bakers/{address}")]
        public async Task<ActionResult<IEnumerable<BakerRewards>>> GetBakerRewards(
            [Required][TzAddress] string address,
            Int32Parameter? cycle,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection,
            Symbols quote = Symbols.None)
        {
            if (await accounts.GetAsync(address) is not RawDelegate baker)
                return Ok(Enumerable.Empty<BakerRewards>());

            var filter = new CycleRewardsFilter { baker = baker, cycle = cycle };

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection), ("quote", quote));

            if (responseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (selection.select == null)
            {
                res = await rewards.GetBakerRewards(filter, pagination, quote);
            }
            else
            {
                res = new SelectionResponse
                {
                    Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                    Rows = await rewards.GetBakerRewards(filter, pagination, selection, quote)
                };
            }
            cached = responseCache.Set(query, res);
            return this.Bytes(cached);
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
        public async Task<int> GetDelegatorRewardsCount([Required][Address] string address)
        {
            if (await accounts.GetAsync(address) is not RawAccount delegator)
                return 0;

            return await rewards.GetDelegatorRewardsCount(new() { delegator = delegator });
        }

        /// <summary>
        /// Get delegator cycle rewards
        /// </summary>
        /// <remarks>
        /// Returns a list of delegator rewards for every cycle, including future cycles.
        /// </remarks>
        /// <param name="address">Delegator address.</param>
        /// <param name="cycle">Filter by cycle.</param>
        /// <param name="pagination"></param>
        /// <param name="selection"></param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("delegators/{address}")]
        public async Task<ActionResult<IEnumerable<DelegatorRewards>>> GetDelegatorRewards(
            [Required][Address] string address,
            Int32Parameter? cycle,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection,
            Symbols quote = Symbols.None)
        {
            if (await accounts.GetAsync(address) is not RawAccount delegator)
                return Ok(Enumerable.Empty<DelegatorRewards>());

            var filter = new DelegatorRewardsFilter { delegator = delegator, cycle = cycle };

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection), ("quote", quote));

            if (responseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (selection.select == null)
            {
                res = await rewards.GetDelegatorRewards(filter, pagination, quote);
            }
            else
            {
                res = new SelectionResponse
                {
                    Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                    Rows = await rewards.GetDelegatorRewards(filter, pagination, selection, quote)
                };
            }
            cached = responseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get staker cycle rewards count
        /// </summary>
        /// <remarks>
        /// Returns total number of cycles where the staker was staking to an active baker
        /// </remarks>
        /// <param name="address">Staker address</param>
        /// <returns></returns>
        [HttpGet("stakers/{address}/count")]
        public async Task<int> GetStakerRewardsCount([Required][Address] string address)
        {
            if (await accounts.GetAsync(address) is not RawAccount staker)
                return 0;

            return await rewards.GetStakerRewardsCount(new() { staker = staker });
        }

        /// <summary>
        /// Get staker cycle rewards
        /// </summary>
        /// <remarks>
        /// Returns a list of staker rewards.
        /// </remarks>
        /// <param name="address">Staker address.</param>
        /// <param name="cycle">Filter by cycle.</param>
        /// <param name="pagination"></param>
        /// <param name="selection"></param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("stakers/{address}")]
        public async Task<ActionResult<IEnumerable<StakerRewards>>> GetStakerRewards(
            [Required][Address] string address,
            Int32Parameter? cycle,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection,
            Symbols quote = Symbols.None)
        {
            if (await accounts.GetAsync(address) is not RawAccount staker)
                return Ok(Enumerable.Empty<StakerRewards>());

            var filter = new StakerRewardsFilter { staker = staker, cycle = cycle };

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
        /// Get reward split
        /// </summary>
        /// <remarks>
        /// Returns baker rewards for the specified cycle with all delegators and stakers (both snapshotted and actual ones).
        /// This endpoint helps to distribute delegation rewards between delegators in proportion to their delegated balances.
        /// </remarks>
        /// <param name="baker">Baker address</param>
        /// <param name="cycle">Rewards cycle</param>
        /// <param name="offset">Specifies how many delegators/stakers in the reward split should be skipped</param>
        /// <param name="limit">Maximum number of delegators/stakers to return</param>
        /// <returns></returns>
        [HttpGet("split/{baker}/{cycle:int}")]
        public Task<RewardSplit?> GetRewardSplit([Required][TzAddress] string baker, [Min(0)] int cycle, int offset = 0, [Range(0, 10000)] int limit = 100)
        {
            return rewards.GetRewardSplit(baker, cycle, offset, limit);
        }

        /// <summary>
        /// Get reward split member
        /// </summary>
        /// <remarks>
        /// Returns delegator/staker from the reward split for the specified cycle.
        /// </remarks>
        /// <param name="baker">Baker address</param>
        /// <param name="cycle">Reward split cycle</param>
        /// <param name="member">Member address</param>
        /// <returns></returns>
        [HttpGet("split/{baker}/{cycle:int}/{member}")]
        public Task<SplitMember?> GetRewardSplitMember([Required][TzAddress] string baker, [Min(0)] int cycle, [Required][Address] string member)
        {
            return rewards.GetRewardSplitMember(baker, cycle, member);
        }
    }
}
