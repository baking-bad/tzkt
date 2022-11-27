using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/rights")]
    public class RightsController : ControllerBase
    {
        private readonly BakingRightsRepository BakingRights;
        public RightsController(BakingRightsRepository bakingRights)
        {
            BakingRights = bakingRights;
        }

        /// <summary>
        /// Get rights count
        /// </summary>
        /// <remarks>
        /// Returns the total number of stored rights.
        /// </remarks>
        /// <param name="type">Filters rights by type (`baking`, `endorsing`)</param>
        /// <param name="baker">Filters rights by baker</param>
        /// <param name="cycle">Filters rights by cycle</param>
        /// <param name="level">Filters rights by level</param>
        /// <param name="slots">Filters rights by slots</param>
        /// <param name="round">Filters rights by round</param>
        /// <param name="priority">[DEPRECATED]</param>
        /// <param name="status">Filters rights by status (`future`, `realized`, `missed`)</param>
        /// <returns></returns>
        [HttpGet("count")]
        public Task<int> GetCount(
            BakingRightTypeParameter type,
            AccountParameter baker,
            Int32Parameter cycle,
            Int32Parameter level,
            Int32NullParameter slots,
            Int32NullParameter round,
            Int32NullParameter priority,
            BakingRightStatusParameter status)
        {
            return BakingRights.GetCount(type, baker, cycle, level, slots, round ?? priority, status);
        }

        /// <summary>
        /// Get rights
        /// </summary>
        /// <remarks>
        /// Returns a list of rights.
        /// </remarks>
        /// <param name="type">Filters rights by type (`baking`, `endorsing`)</param>
        /// <param name="baker">Filters rights by baker</param>
        /// <param name="cycle">Filters rights by cycle</param>
        /// <param name="level">Filters rights by level</param>
        /// <param name="slots">Filters rights by slots</param>
        /// <param name="round">Filters rights by round</param>
        /// <param name="priority">[DEPRECATED]</param>
        /// <param name="status">Filters rights by status (`future`, `realized`, `missed`)</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts rights by specified field. Supported fields: `level` (default).</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BakingRight>>> Get(
            BakingRightTypeParameter type,
            AccountParameter baker,
            Int32Parameter cycle,
            Int32Parameter level,
            Int32NullParameter slots,
            Int32NullParameter round,
            Int32NullParameter priority,
            BakingRightStatusParameter status,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await BakingRights.Get(type, baker, cycle, level, slots, round ?? priority, status, sort, offset, limit));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await BakingRights.Get(type, baker, cycle, level, slots, round ?? priority, status, sort, offset, limit, select.Values[0]));
                else
                    return Ok(await BakingRights.Get(type, baker, cycle, level, slots, round ?? priority, status, sort, offset, limit, select.Values));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await BakingRights.Get(type, baker, cycle, level, slots, round ?? priority, status, sort, offset, limit, select.Fields[0]));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await BakingRights.Get(type, baker, cycle, level, slots, round ?? priority, status, sort, offset, limit, select.Fields)
                    });
                }
            }
        }

        [OpenApiIgnore]
        [HttpGet("schedule")]
        public async Task<ActionResult<IEnumerable<BakingRight>>> GetSchedule(
            [Required][TzAddress] string baker,
            [Required] DateTimeOffset from,
            [Required] DateTimeOffset to,
            [Min(0)] int maxRound = 0)
        {
            #region validate
            if (to <= from)
                return new BadRequest(nameof(to), "'to' should be greater than 'from'");

            if ((to - from).TotalDays > 14)
                return new BadRequest(nameof(to), "Maximum schedule period is 14 days");
            #endregion

            return Ok(await BakingRights.GetSchedule(baker, from.UtcDateTime, to.UtcDateTime, maxRound));
        }
    }
}
