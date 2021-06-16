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
    [Route("v1/software")]
    public class SoftwareController : ControllerBase
    {
        private readonly SoftwareRepository Software;

        public SoftwareController(SoftwareRepository software)
        {
            Software = software;
        }

        /// <summary>
        /// Get baker software
        /// </summary>
        /// <remarks>
        /// Returns a list of baker software.
        /// </remarks>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `id` (default), `firstLevel`, `lastLevel`, `blocksCount`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Software>>> Get(
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("firstLevel", "lastLevel", "blocksCount"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Software.Get(sort, offset, limit));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Software.Get(sort, offset, limit, select.Values[0]));
                else
                    return Ok(await Software.Get(sort, offset, limit, select.Values));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Software.Get(sort, offset, limit, select.Fields[0]));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Software.Get(sort, offset, limit, select.Fields)
                    });
                }
            }
        }

        /// <summary>
        /// Get software count
        /// </summary>
        /// <remarks>
        /// Returns a number of software.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("count")]
        public Task<int> GetCount()
        {
            return Software.GetCount();
        }
    }
}
