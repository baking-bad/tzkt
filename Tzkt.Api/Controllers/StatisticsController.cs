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
    [Route("v1/statistics")]
    public class StatisticsController : ControllerBase
    {
        readonly StateCache State;
        readonly StatisticsRepository Statistics;

        public StatisticsController(StateCache state, StatisticsRepository statistics)
        {
            State = state;
            Statistics = statistics;
        }

        /// <summary>
        /// Get statistics
        /// </summary>
        /// <remarks>
        /// Returns a list of end-of-block statistics.
        /// </remarks>
        /// <param name="level">Filters statistics by level.</param>
        /// <param name="timestamp">Filters statistics by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `id` (default), `level`, `cycle`, `date`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Statistics>>> Get(
            Int32Parameter level,
            TimestampParameter timestamp,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "cycle", "date"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Statistics.Get(StatisticsPeriod.None, null, level, timestamp, null, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Statistics.Get(StatisticsPeriod.None, null, level, timestamp, null, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Statistics.Get(StatisticsPeriod.None, null, level, timestamp, null, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Statistics.Get(StatisticsPeriod.None, null, level, timestamp, null, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Statistics.Get(StatisticsPeriod.None, null, level, timestamp, null, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get daily statistics
        /// </summary>
        /// <remarks>
        /// Returns a list of end-of-day statistics.
        /// </remarks>
        /// <param name="date">Filters statistics by date.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `id` (default), `level`, `cycle`, `date`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("daily")]
        public async Task<ActionResult<IEnumerable<Statistics>>> GetDaily(
            DateTimeParameter date,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "cycle", "date"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Statistics.Get(StatisticsPeriod.Daily, null, null, null, date, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Statistics.Get(StatisticsPeriod.Daily, null, null, null, date, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Statistics.Get(StatisticsPeriod.Daily, null, null, null, date, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Statistics.Get(StatisticsPeriod.Daily, null, null, null, date, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Statistics.Get(StatisticsPeriod.Daily, null, null, null, date, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get cyclic statistics
        /// </summary>
        /// <remarks>
        /// Returns a list of end-of-cycle statistics.
        /// </remarks>
        /// <param name="cycle">Filters statistics by cycle.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `id` (default), `level`, `cycle`, `date`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("cyclic")]
        public async Task<ActionResult<IEnumerable<Statistics>>> GetCycles(
            Int32Parameter cycle,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "cycle", "date"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Statistics.Get(StatisticsPeriod.Cyclic, cycle, null, null, null, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Statistics.Get(StatisticsPeriod.Cyclic, cycle, null, null, null, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Statistics.Get(StatisticsPeriod.Cyclic, cycle, null, null, null, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Statistics.Get(StatisticsPeriod.Cyclic, cycle, null, null, null, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Statistics.Get(StatisticsPeriod.Cyclic, cycle, null, null, null, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get current statistics
        /// </summary>
        /// <remarks>
        /// Returns statistics at the end of a head block.
        /// </remarks>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select a single field, response will be a single value in both `.fields` and `.values` modes.</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("current")]
        public async Task<ActionResult<Statistics>> GetCycles(SelectParameter select, Symbols quote = Symbols.None)
        {
            var level = new Int32Parameter { Eq = State.Current.Level };

            if (select == null)
                return Ok((await Statistics.Get(StatisticsPeriod.None, null, level, null, null, null, null, 1, quote)).FirstOrDefault());

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok((await Statistics.Get(StatisticsPeriod.None, null, level, null, null, null, null, 1, select.Values[0], quote)).FirstOrDefault());
                else
                    return Ok((await Statistics.Get(StatisticsPeriod.None, null, level, null, null, null, null, 1, select.Values, quote)).FirstOrDefault());
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok((await Statistics.Get(StatisticsPeriod.None, null, level, null, null, null, null, 1, select.Fields[0], quote)).FirstOrDefault());
                else
                {
                    return Ok(new SelectionSingleResponse
                    {
                        Cols = select.Fields,
                        Vals = (await Statistics.Get(StatisticsPeriod.None, null, level, null, null, null, null, 1, select.Fields, quote)).FirstOrDefault()
                    });
                }
            }
        }
    }
}
