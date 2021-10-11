using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/constants")]
    public class ConstantsController : ControllerBase
    {
        private readonly ConstantsRepository Constants;
        private readonly StateCache State;

        public ConstantsController(ConstantsRepository constants, StateCache state)
        {
            Constants = constants;
            State = state;
        }

        /// <summary>
        /// Get global constants
        /// </summary>
        /// <remarks>
        /// Returns a list of global constants.
        /// </remarks>
        /// <param name="address">Filters constants by global address (starts with `expr..`).</param>
        /// <param name="creationLevel">Filters constants by creation level.</param>
        /// <param name="creationTime">Filters constants by creation time.</param>
        /// <param name="creator">Filters constants by creator.</param>
        /// <param name="refs">Filters constants by number of refs.</param>
        /// <param name="size">Filters constants by size in bytes.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `id` (default), `creationLevel`, `size`, `refs`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="format">Constant value format (`0` - micheline, `1` - michelson, `2` - bytes (base64))</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Constant>>> Get(
            ExpressionParameter address,
            Int32Parameter creationLevel,
            TimestampParameter creationTime,
            AccountParameter creator,
            Int32Parameter refs,
            Int32Parameter size,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            [Range(0, 2)] int format = 0)
        {
            #region validate
            if (sort != null && !sort.Validate("creationLevel", "size", "refs"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Constants.Get(address, creationLevel, creationTime, creator, refs, size, sort, offset, limit, format));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Constants.Get(address, creationLevel, creationTime, creator, refs, size, sort, offset, limit, select.Values[0], format));
                else
                    return Ok(await Constants.Get(address, creationLevel, creationTime, creator, refs, size, sort, offset, limit, select.Values, format));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Constants.Get(address, creationLevel, creationTime, creator, refs, size, sort, offset, limit, select.Fields[0], format));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Constants.Get(address, creationLevel, creationTime, creator, refs, size, sort, offset, limit, select.Fields, format)
                    });
                }
            }
        }

        /// <summary>
        /// Get global constant by address
        /// </summary>
        /// <remarks>
        /// Returns global constant with specified address (expression hash).
        /// </remarks>
        /// <param name="address">Global address (starts with `expr..`)</param>
        /// <param name="format">Constant value format (`0` - micheline, `1` - michelson, `2` - bytes (base64))</param>
        /// <returns></returns>
        [HttpGet("{address}")]
        public async Task<Constant> GetByAddress(
            [Required][ExpressionHash] string address, [Range(0, 2)] int format = 0)
        {
            var res = await Constants.Get(address, null, null, null, null, null, null, null, 1, format);
            return res.FirstOrDefault();
        }

        /// <summary>
        /// Get global constants count
        /// </summary>
        /// <remarks>
        /// Returns a number of global constants.
        /// </remarks>
        /// <param name="refs">Filters constants by number of refs.</param>
        /// <returns></returns>
        [HttpGet("count")]
        public Task<int> GetCount(Int32Parameter refs)
        {
            if (refs == null)
                return Task.FromResult(State.Current.ConstantsCount);

            return Constants.GetCount();
        }
    }
}
