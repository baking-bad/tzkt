using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/contracts/events")]
    public class EventsController : ControllerBase
    {
        readonly ContractEventsRepository Events;
        readonly StateCache State;
        readonly ResponseCacheService ResponseCache;

        public EventsController(ContractEventsRepository events, StateCache state, ResponseCacheService responseCache)
        {
            Events = events;
            State = state;
            ResponseCache = responseCache;
        }

        /// <summary>
        /// Get contract events count
        /// </summary>
        /// <remarks>
        /// Returns a total number of contract events.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetContractEventsCount([FromQuery] ContractEventFilter filter)
        {
            if (filter.id != null ||
                filter.level != null ||
                filter.timestamp != null ||
                filter.contract != null ||
                filter.codeHash != null ||
                filter.tag != null ||
                filter.payload != null ||
                filter.transactionId != null)
            {
                var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

                if (ResponseCache.TryGet(query, out var cached))
                    return this.Bytes(cached);

                var res = await Events.GetContractEventsCount(filter);
                cached = ResponseCache.Set(query, res);
                return this.Bytes(cached);
            }

            return Ok(State.Current.EventsCount);
        }

        /// <summary>
        /// Get contract events
        /// </summary>
        /// <remarks>
        /// Returns a list of contract events.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContractEvent>>> GetContractEvents(
            [FromQuery] ContractEventFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (selection.select == null)
            {
                res = await Events.GetContractEvents(filter, pagination);
            }
            else
            {
                res = new SelectionResponse
                {
                    Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                    Rows = await Events.GetContractEvents(filter, pagination, selection.select.Fields ?? selection.select.Values)
                };
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
    }
}
