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
    [Route("v1/cycles")]
    public class CyclesController : ControllerBase
    {
        private readonly CyclesRepository Cycles;
        public CyclesController(CyclesRepository cycles)
        {
            Cycles = cycles;
        }

        /// <summary>
        /// Get cycles count
        /// </summary>
        /// <remarks>
        /// Returns the total number of cycles, including future cycles.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("count")]
        public Task<int> GetCount()
        {
            return Cycles.GetCount();
        }

        /// <summary>
        /// Get cycles
        /// </summary>
        /// <remarks>
        /// Returns a list of cycles, including future cycles.
        /// </remarks>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.rec` and `.tup` modes.</param>
        /// <param name="sort">Sorts cycles by specified field. Supported fields: `index`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cycle>>> Get(
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("index"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Cycles.Get(sort, offset, limit));

            if (select.Tup != null)
            {
                if (select.Tup.Length == 1)
                    return Ok(await Cycles.Get(sort, offset, limit, select.Tup[0]));
                else
                    return Ok(await Cycles.Get(sort, offset, limit, select.Tup));
            }
            else
            {
                if (select.Rec.Length == 1)
                    return Ok(await Cycles.Get(sort, offset, limit, select.Rec[0]));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Rec,
                        Rows = await Cycles.Get(sort, offset, limit, select.Rec)
                    });
                }
            }
        }

        /// <summary>
        /// Get cycle by index
        /// </summary>
        /// <remarks>
        /// Returns a cycle at the specified index.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("{index:int}")]
        public Task<Cycle> GetByIndex(int index)
        {
            return Cycles.Get(index);
        }
    }
}
