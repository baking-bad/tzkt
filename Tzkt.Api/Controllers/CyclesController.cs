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
    [Route("v1/cycles")]
    public class CyclesController : ControllerBase
    {
        private readonly CyclesRepository Cycles;
        private readonly StateCache State;

        public CyclesController(CyclesRepository cycles, StateCache state)
        {
            Cycles = cycles;
            State = state;
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
            return Task.FromResult(State.Current.CyclesCount);
        }

        /// <summary>
        /// Get cycles
        /// </summary>
        /// <remarks>
        /// Returns a list of cycles, including future cycles.
        /// </remarks>
        /// <param name="snapshotIndex">Filters cycles by snapshot index (0..15)</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts cycles by specified field. Supported fields: `index` (default, desc).</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cycle>>> Get(
            Int32Parameter snapshotIndex,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("index"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Cycles.Get(snapshotIndex, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Cycles.Get(snapshotIndex, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Cycles.Get(snapshotIndex, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Cycles.Get(snapshotIndex, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Cycles.Get(snapshotIndex, sort, offset, limit, select.Fields, quote)
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
        /// <param name="index">Cycle index starting from zero</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{index:int}")]
        public Task<Cycle> GetByIndex([Min(0)] int index, Symbols quote = Symbols.None)
        {
            return Cycles.Get(index, quote);
        }
    }
}
