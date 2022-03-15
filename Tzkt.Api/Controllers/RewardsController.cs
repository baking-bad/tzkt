using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/rewards")]
    public class RewardsController : ControllerBase
    {
        private readonly RewardsRepository Rewards;

        public RewardsController(RewardsRepository rewards)
        {
            Rewards = rewards;
        }

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
            return Rewards.GetBakerRewardsCount(address);
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
            Int32Parameter cycle,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("cycle"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Rewards.GetBakerRewards(address, cycle, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Rewards.GetBakerRewards(address, cycle, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Rewards.GetBakerRewards(address, cycle, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Rewards.GetBakerRewards(address, cycle, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Rewards.GetBakerRewards(address, cycle, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get baker cycle rewards by cycle
        /// </summary>
        /// <remarks>
        /// Returns baker cycle rewards for the specified cycle.
        /// </remarks>
        /// <param name="address">Baker address</param>
        /// <param name="cycle">Rewards cycle</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("bakers/{address}/{cycle:int}")]
        public Task<BakerRewards> GetBakerRewardsByCycle([Required][TzAddress] string address, [Min(0)] int cycle, Symbols quote = Symbols.None)
        {
            return Rewards.GetBakerRewards(address, cycle, quote);
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
            return Rewards.GetDelegatorRewardsCount(address);
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
            Int32Parameter cycle,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("cycle"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Rewards.GetDelegatorRewards(address, cycle, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Rewards.GetDelegatorRewards(address, cycle, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Rewards.GetDelegatorRewards(address, cycle, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Rewards.GetDelegatorRewards(address, cycle, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Rewards.GetDelegatorRewards(address, cycle, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get delegator cycle rewards by cycle
        /// </summary>
        /// <remarks>
        /// Returns delegator cycle rewards for the specified cycle.
        /// </remarks>
        /// <param name="address">Delegator address</param>
        /// <param name="cycle">Rewards cycle</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("delegators/{address}/{cycle:int}")]
        public Task<DelegatorRewards> GetDelegatorRewardsByCycle([Required][Address] string address, [Min(0)] int cycle, Symbols quote = Symbols.None)
        {
            return Rewards.GetDelegatorRewards(address, cycle, quote);
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
        public Task<RewardSplit> GetRewardSplit([Required][TzAddress] string baker, [Min(0)] int cycle, int offset = 0, [Range(0, 10000)] int limit = 100)
        {
            return Rewards.GetRewardSplit(baker, cycle, offset, limit);
        }

        /// <summary>
        /// Get reward split delegator
        /// </summary>
        /// <remarks>
        /// Returns delegator from the reward split for the specified cycle.
        /// </remarks>
        /// <param name="baker">Baker address</param>
        /// <param name="cycle">Reward split cycle</param>
        /// <param name="delegator">Delegator address</param>
        /// <returns></returns>
        [HttpGet("split/{baker}/{cycle:int}/{delegator}")]
        public Task<SplitDelegator> GetRewardSplitDelegator([Required][TzAddress] string baker, [Min(0)] int cycle, [Required][Address] string delegator)
        {
            return Rewards.GetRewardSplitDelegator(baker, cycle, delegator);
        }
    }
}
