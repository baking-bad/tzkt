using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/tickets")]
    public class TicketsController : ControllerBase
    {
        readonly TicketsRepository Tickets;
        readonly StateCache State;
        readonly ResponseCacheService ResponseCache;

        public TicketsController(TicketsRepository tickets, StateCache state, ResponseCacheService responseCache)
        {
            Tickets = tickets;
            State = state;
            ResponseCache = responseCache;
        }

        #region tickets
        /// <summary>
        /// Get tickets count
        /// </summary>
        /// <remarks>
        /// Returns a total number of tickets.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetTicketsCount([FromQuery] TicketFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.TicketsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Tickets.GetTicketsCount(filter);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get tickets
        /// </summary>
        /// <remarks>
        /// Returns a list of tickets.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ticket>>> GetTickets(
            [FromQuery] TicketFilter filter,
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
                res = await Tickets.GetTickets(filter, pagination);
            }
            else
            {
                res = new SelectionResponse
                {
                    Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                    Rows = await Tickets.GetTickets(filter, pagination, selection.select.Fields ?? selection.select.Values)
                };
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region ticket balances
        /// <summary>
        /// Get ticket balances count
        /// </summary>
        /// <remarks>
        /// Returns a total number of ticket balances.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("balances/count")]
        public async Task<ActionResult<int>> GetTicketBalancesCount([FromQuery] TicketBalanceFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.TicketBalancesCount);

            #region optimizations
            if (filter.account != null && (filter.account.Eq == -1 || filter.account.In?.Count == 0 && !filter.account.InHasNull))
                return Ok(0);
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);
                
            var res = await Tickets.GetTicketBalancesCount(filter);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get ticket balances
        /// </summary>
        /// <remarks>
        /// Returns a list of ticket balances.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <returns></returns>
        [HttpGet("balances")]
        public async Task<ActionResult<IEnumerable<TicketBalance>>> GetTicketBalances(
            [FromQuery] TicketBalanceFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection)
        {
            #region optimizations
            if (filter.account != null && (filter.account.Eq == -1 || filter.account.In?.Count == 0 && !filter.account.InHasNull))
                return Ok(Enumerable.Empty<TicketBalance>());
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value, 
                ("filter", filter), ("pagination", pagination), ("selection", selection));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);
            
            object res;
            if (selection.select == null)
            {
                res = await Tickets.GetTicketBalances(filter, pagination);
            }
            else
            {
                res = new SelectionResponse
                {
                    Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                    Rows = await Tickets.GetTicketBalances(filter, pagination, selection.select.Fields ?? selection.select.Values)
                };
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region ticket transfers
        /// <summary>
        /// Get ticket transfers count
        /// </summary>
        /// <remarks>
        /// Returns the total number of ticket transfers.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("transfers/count")]
        public async Task<ActionResult<int>> GetTicketTransfersCount([FromQuery] TicketTransferFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.TicketTransfersCount);

            #region optimizations
            if (filter.from != null && (filter.from.Eq == -1 || filter.from.In?.Count == 0 && !filter.from.InHasNull))
                return Ok(0);

            if (filter.to != null && (filter.to.Eq == -1 || filter.to.In?.Count == 0 && !filter.to.InHasNull))
                return Ok(0);

            if (filter.anyof != null && (filter.anyof.Eq == -1 || filter.anyof.In?.Count == 0 && !filter.anyof.InHasNull))
                return Ok(0);
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Tickets.GetTicketTransfersCount(filter);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get ticket transfers
        /// </summary>
        /// <remarks>
        /// Returns a list of ticket transfers.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <returns></returns>
        [HttpGet("transfers")]
        public async Task<ActionResult<IEnumerable<TicketTransfer>>> GetTicketTransfers(
            [FromQuery] TicketTransferFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection)
        {
            #region optimizations
            if (filter.from != null && (filter.from.Eq == -1 || filter.from.In?.Count == 0 && !filter.from.InHasNull))
                return Ok(Enumerable.Empty<TicketTransfer>());

            if (filter.to != null && (filter.to.Eq == -1 || filter.to.In?.Count == 0 && !filter.to.InHasNull))
                return Ok(Enumerable.Empty<TicketTransfer>());

            if (filter.anyof != null && (filter.anyof.Eq == -1 || filter.anyof.In?.Count == 0 && !filter.anyof.InHasNull))
                return Ok(Enumerable.Empty<TicketTransfer>());
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (selection.select == null)
            {
                res = await Tickets.GetTicketTransfers(filter, pagination);
            }
            else
            {
                res = new SelectionResponse
                {
                    Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                    Rows = await Tickets.GetTicketTransfers(filter, pagination, selection.select.Fields ?? selection.select.Values)
                };
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region historical balances
        /// <summary>
        /// Get historical ticket balances
        /// </summary>
        /// <remarks>
        /// Returns a list of ticket balances at the end of the specified block.
        /// Note, this endpoint is quite heavy, therefore at least one of the filters
        /// (`account`, `ticket.id`, `ticket.ticketer`) must be specified.
        /// </remarks>
        /// <param name="level">Level of the block at the end of which historical balances must be calculated</param>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <returns></returns>
        [HttpGet("historical_balances/{level:int}")]
        public async Task<ActionResult<IEnumerable<TicketBalanceShort>>> GetTicketBalances(int level,
            [FromQuery] TicketBalanceShortFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection)
        {

            if (filter.account?.Eq == null &&
                filter.account?.In == null &&
                filter.ticket.id?.Eq == null &&
                filter.ticket.id?.In == null &&
                filter.ticket.ticketer?.Eq == null &&
                filter.ticket.ticketer?.In == null)
                return new BadRequest("query", "At least one of the filters (`account`, `ticket.id`, `ticket.ticketer`) must be specified");

            #region optimizations
            if (filter.account != null && (filter.account.Eq == -1 || filter.account.In?.Count == 0 && !filter.account.InHasNull))
                return Ok(Enumerable.Empty<TicketBalanceShort>());
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (selection.select == null)
            {
                res = await Tickets.GetHistoricalTicketBalances(level, filter, pagination);
            }
            else
            {
                res = new SelectionResponse
                {
                    Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                    Rows = await Tickets.GetHistoricalTicketBalances(level, filter, pagination, selection.select.Fields ?? selection.select.Values)
                };
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion
    }
}
