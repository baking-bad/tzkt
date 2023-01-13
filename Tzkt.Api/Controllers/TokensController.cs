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
    [Route("v1/tokens")]
    public class TokensController : ControllerBase
    {
        readonly TokensRepository Tokens;
        readonly StateCache State;
        readonly ResponseCacheService ResponseCache;

        public TokensController(TokensRepository tokens, StateCache state, ResponseCacheService responseCache)
        {
            Tokens = tokens;
            State = state;
            ResponseCache = responseCache;
        }

        #region tokens
        /// <summary>
        /// Get tokens count
        /// </summary>
        /// <remarks>
        /// Returns a total number of tokens.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetTokensCount([FromQuery] TokenFilter filter)
        {
            if (filter.contract != null ||
                filter.metadata != null ||
                filter.standard != null ||
                filter.firstTime != null ||
                filter.firstMinter != null ||
                filter.firstLevel != null ||
                filter.lastTime != null ||
                filter.lastLevel != null ||
                filter.tokenId != null ||
                filter.id != null ||
                filter.indexedAt != null)
            {
                var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

                if (ResponseCache.TryGet(query, out var cached))
                    return this.Bytes(cached);

                var res = await Tokens.GetTokensCount(filter);
                cached = ResponseCache.Set(query, res);
                return this.Bytes(cached);
            }

            return Ok(State.Current.TokensCount);
        }

        /// <summary>
        /// Get tokens
        /// </summary>
        /// <remarks>
        /// Returns a list of tokens.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Token>>> GetTokens(
            [FromQuery] TokenFilter filter,
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
                res = await Tokens.GetTokens(filter, pagination);
            }
            else
            {
                res = new SelectionResponse
                {
                    Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                    Rows = await Tokens.GetTokens(filter, pagination, selection.select.Fields ?? selection.select.Values)
                };
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region token balances
        /// <summary>
        /// Get token balances count
        /// </summary>
        /// <remarks>
        /// Returns a total number of token balances.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("balances/count")]
        public async Task<ActionResult<int>> GetTokenBalancesCount([FromQuery] TokenBalanceFilter filter)
        {
            if (filter.account != null ||
                filter.balance != null ||
                filter.firstTime != null ||
                filter.firstLevel != null ||
                filter.lastTime != null ||
                filter.lastLevel != null ||
                filter.indexedAt != null ||
                filter.id != null ||
                filter.token.id != null ||
                filter.token.contract != null ||
                filter.token.tokenId != null ||
                filter.token.standard != null ||
                filter.token.metadata != null)
            {
                #region optimizations
                if (filter.account != null && (filter.account.Eq == -1 || filter.account.In?.Count == 0 && !filter.account.InHasNull))
                    return Ok(0);
                #endregion

                var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

                if (ResponseCache.TryGet(query, out var cached))
                    return this.Bytes(cached);
                
                var res = await Tokens.GetTokenBalancesCount(filter);
                cached = ResponseCache.Set(query, res);
                return this.Bytes(cached);
            }

            return Ok(State.Current.TokenBalancesCount);
        }

        /// <summary>
        /// Get token balances
        /// </summary>
        /// <remarks>
        /// Returns a list of token balances.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <returns></returns>
        [HttpGet("balances")]
        public async Task<ActionResult<IEnumerable<TokenBalance>>> GetTokenBalances(
            [FromQuery] TokenBalanceFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection)
        {
            #region optimizations
            if (filter.account != null && (filter.account.Eq == -1 || filter.account.In?.Count == 0 && !filter.account.InHasNull))
                return Ok(Enumerable.Empty<TokenBalance>());
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value, 
                ("filter", filter), ("pagination", pagination), ("selection", selection));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);
            
            object res;
            if (selection.select == null)
            {
                res = await Tokens.GetTokenBalances(filter, pagination);
            }
            else
            {
                res = new SelectionResponse
                {
                    Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                    Rows = await Tokens.GetTokenBalances(filter, pagination, selection.select.Fields ?? selection.select.Values)
                };
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region token transfers
        /// <summary>
        /// Get token transfers count
        /// </summary>
        /// <remarks>
        /// Returns the total number of token transfers.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("transfers/count")]
        public async Task<ActionResult<int>> GetTokenTransfersCount([FromQuery] TokenTransferFilter filter)
        {
            if (filter.level != null ||
                filter.timestamp != null ||
                filter.from != null ||
                filter.to != null ||
                filter.anyof != null ||
                filter.amount != null ||
                filter.id != null ||
                filter.transactionId != null ||
                filter.originationId != null ||
                filter.migrationId != null ||
                filter.indexedAt != null ||
                filter.token.id != null ||
                filter.token.contract != null ||
                filter.token.tokenId != null ||
                filter.token.standard != null ||
                filter.token.metadata != null)
            {
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

                var res = await Tokens.GetTokenTransfersCount(filter);
                cached = ResponseCache.Set(query, res);
                return this.Bytes(cached);
            }

            return Ok(State.Current.TokenTransfersCount);
        }

        /// <summary>
        /// Get token transfers
        /// </summary>
        /// <remarks>
        /// Returns a list of token transfers.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <returns></returns>
        [HttpGet("transfers")]
        public async Task<ActionResult<IEnumerable<TokenTransfer>>> GetTokenTransfers(
            [FromQuery] TokenTransferFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection)
        {
            #region optimizations
            if (filter.from != null && (filter.from.Eq == -1 || filter.from.In?.Count == 0 && !filter.from.InHasNull))
                return Ok(Enumerable.Empty<TokenTransfer>());

            if (filter.to != null && (filter.to.Eq == -1 || filter.to.In?.Count == 0 && !filter.to.InHasNull))
                return Ok(Enumerable.Empty<TokenTransfer>());

            if (filter.anyof != null && (filter.anyof.Eq == -1 || filter.anyof.In?.Count == 0 && !filter.anyof.InHasNull))
                return Ok(Enumerable.Empty<TokenTransfer>());
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (selection.select == null)
            {
                res = await Tokens.GetTokenTransfers(filter, pagination);
            }
            else
            {
                res = new SelectionResponse
                {
                    Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                    Rows = await Tokens.GetTokenTransfers(filter, pagination, selection.select.Fields ?? selection.select.Values)
                };
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region historical balances
        /// <summary>
        /// Get historical token balances
        /// </summary>
        /// <remarks>
        /// Returns a list of token balances at the end of the specified block.
        /// Note, this endpoint is quite heavy, therefore at least one of the filters
        /// (`account`, `token.id`, `token.contract` with `token.tokenId`) must be specified.
        /// </remarks>
        /// <param name="level">Level of the block at the end of which historical balances must be calculated</param>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <returns></returns>
        [HttpGet("historical_balances/{level:int}")]
        public async Task<ActionResult<IEnumerable<TokenBalanceShort>>> GetTokenBalances(int level,
            [FromQuery] TokenBalanceShortFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection)
        {
            if (filter.account?.Eq == null &&
                filter.account?.In == null &&
                filter.token.id?.Eq == null &&
                filter.token.id?.In == null &&
                (filter.token.contract?.Eq == null && filter.token.contract?.In == null ||
                filter.token.tokenId?.Eq == null && filter.token.tokenId?.In == null))
                return new BadRequest("query", "At least one of the filters (`account`, `token.id`, `token.contract` with `token.tokenId`) must be specified");

            #region optimizations
            if (filter.account != null && (filter.account.Eq == -1 || filter.account.In?.Count == 0 && !filter.account.InHasNull))
                return Ok(Enumerable.Empty<TokenBalanceShort>());
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (selection.select == null)
            {
                res = await Tokens.GetHistoricalTokenBalances(level, filter, pagination);
            }
            else
            {
                res = new SelectionResponse
                {
                    Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                    Rows = await Tokens.GetHistoricalTokenBalances(level, filter, pagination, selection.select.Fields ?? selection.select.Values)
                };
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion
    }
}
