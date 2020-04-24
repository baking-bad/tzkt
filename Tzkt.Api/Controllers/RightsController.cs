using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

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
        /// <param name="priority">Filters rights by priority</param>
        /// <param name="status">Filters rights by status (`future`, `success`, `missed`)</param>
        /// <returns></returns>
        [HttpGet("count")]
        public Task<int> GetCount(
            BakingRightTypeParameter type,
            AccountParameter baker,
            Int32Parameter cycle,
            Int32Parameter level,
            Int32Parameter slots,
            Int32Parameter priority,
            BakingRightStatusParameter status)
        {
            return BakingRights.GetCount(type, baker, cycle, level, slots, priority, status);
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
        /// <param name="priority">Filters rights by priority</param>
        /// <param name="status">Filters rights by status (`future`, `success`, `missed`)</param>
        /// <param name="sort">Sorts rights by specified field. Supported fields: `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BakingRight>>> Get(
            BakingRightTypeParameter type,
            AccountParameter baker,
            Int32Parameter cycle,
            Int32Parameter level,
            Int32Parameter slots,
            Int32Parameter priority,
            BakingRightStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null)
        {
            #region validate
            if (sort != null && !sort.Validate("level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (string.IsNullOrEmpty(select))
                return Ok(await BakingRights.Get(type, baker, cycle, level, slots, priority, status, sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await BakingRights.Get(type, baker, cycle, level, slots, priority, status, sort, offset, limit, fields[0]));

            return Ok(await BakingRights.Get(type, baker, cycle, level, slots, priority, status, sort, offset, limit, fields));
        }
    }
}
