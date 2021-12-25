using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/tokens")]
    public class TokensController : ControllerBase
    {
        readonly TokensRepository Tokens;
        readonly StateCache State;

        public TokensController(TokensRepository tokens, StateCache state)
        {
            Tokens = tokens;
            State = state;
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
        public Task<int> GetTokensCount([FromQuery] TokenFilter filter)
        {
            if (filter.contract != null ||
                filter.metadata != null ||
                filter.standard != null ||
                filter.firstTime != null ||
                filter.firstLevel != null ||
                filter.lastTime != null ||
                filter.lastLevel != null ||
                filter.tokenId != null ||
                filter.id != null)
                return Tokens.GetTokensCount(filter);

            return Task.FromResult(State.Current.TokensCount);
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
            if (selection.select == null)
                return Ok(await Tokens.GetTokens(filter, pagination));
            
            return Ok(new SelectionResponse
            {
                Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                Rows = await Tokens.GetTokens(filter, pagination, selection.select.Fields ?? selection.select.Values)
            });
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
        public Task<int> GetTokenBalancesCount([FromQuery] TokenBalanceFilter filter)
        {
            if (filter.account != null ||
                filter.balance != null ||
                filter.firstTime != null ||
                filter.firstLevel != null ||
                filter.lastTime != null ||
                filter.lastLevel != null ||
                filter.id != null ||
                filter.token.id != null ||
                filter.token.contract != null ||
                filter.token.tokenId != null ||
                filter.token.standard != null ||
                filter.token.metadata != null)
                return Tokens.GetTokenBalancesCount(filter);

            return Task.FromResult(State.Current.TokenBalancesCount);
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
            if (selection.select == null)
                return Ok(await Tokens.GetTokenBalances(filter, pagination));

            return Ok(new SelectionResponse
            {
                Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                Rows = await Tokens.GetTokenBalances(filter, pagination, selection.select.Fields ?? selection.select.Values)
            });
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
        public Task<int> GetTokenTransfersCount([FromQuery] TokenTransferFilter filter)
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
                filter.token.id != null ||
                filter.token.contract != null ||
                filter.token.tokenId != null ||
                filter.token.standard != null ||
                filter.token.metadata != null)
                return Tokens.GetTokenTransfersCount(filter);

            return Task.FromResult(State.Current.TokenTransfersCount);
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
            if (selection.select == null)
                return Ok(await Tokens.GetTokenTransfers(filter, pagination));

            return Ok(new SelectionResponse
            {
                Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                Rows = await Tokens.GetTokenTransfers(filter, pagination, selection.select.Fields ?? selection.select.Values)
            });
        }
        #endregion
    }
}
