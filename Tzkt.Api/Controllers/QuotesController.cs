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
    [Route("v1/quotes")]
    public class QuotesController : ControllerBase
    {
        private readonly QuotesRepository Quotes;
        public QuotesController(QuotesRepository quotes)
        {
            Quotes = quotes;
        }

        /// <summary>
        /// Get quotes count
        /// </summary>
        /// <remarks>
        /// Returns the total number of quotes aligned with blocks.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("count")]
        public int GetCount()
        {
            return Quotes.GetCount();
        }

        /// <summary>
        /// Get last quote
        /// </summary>
        /// <remarks>
        /// Returns last known quote.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("last")]
        public Quote GetLast()
        {
            return Quotes.GetLast();
        }

        /// <summary>
        /// Get quotes
        /// </summary>
        /// <remarks>
        /// Returns a list of quotes aligned with blocks.
        /// </remarks>
        /// <param name="level">Filters quotes by level.</param>
        /// <param name="timestamp">Filters quotes by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts quotes by specified field. Supported fields: `level` (default).</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Quote>>> Get(
            Int32Parameter level,
            DateTimeParameter timestamp,
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
                return Ok(await Quotes.Get(level, timestamp, sort, offset, limit));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Quotes.Get(level, timestamp, sort, offset, limit, select.Values[0]));
                else
                    return Ok(await Quotes.Get(level, timestamp, sort, offset, limit, select.Values));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Quotes.Get(level, timestamp, sort, offset, limit, select.Fields[0]));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Quotes.Get(level, timestamp, sort, offset, limit, select.Fields)
                    });
                }
            }
        }
    }
}
