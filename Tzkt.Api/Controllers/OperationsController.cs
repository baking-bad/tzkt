﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/operations")]
    public class OperationsController : ControllerBase
    {
        readonly OperationRepository Operations;
        readonly StateCache State;
        readonly ResponseCacheService ResponseCache;

        public OperationsController(OperationRepository operations, StateCache state, ResponseCacheService responseCache)
        {
            Operations = operations;
            State = state;
            ResponseCache = responseCache;
        }

        #region operations
        /// <summary>
        /// Get operations by hash
        /// </summary>
        /// <remarks>
        /// Returns a list of operations with the specified hash.  
        /// **NOTE: if you know in advance what operation type you want to get (e.g. transactions), prefer using `/v1/operations/{type}/{hash}`
        /// (e.g. [/v1/operations/transactions/{hash}](#operation/Operations_GetTransactionByHash)) instead, because it's much more efficient.**
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{hash}")]
        public async Task<ActionResult<IEnumerable<Operation>>> GetByHash(
            [Required][OpHash] string hash,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("micheline", micheline), ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.Get(hash, micheline, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get operations by hash and counter
        /// </summary>
        /// <remarks>
        /// Returns a list of operations with the specified hash and counter.  
        /// **NOTE: if you know in advance what operation type you want to get (e.g. transactions), prefer using `/v1/operations/{type}/{hash}/{counter}`
        /// (e.g. [/v1/operations/transactions/{hash}/{counter}](#operation/Operations_GetTransactionByHashCounter)) instead, because it's much more efficient.**
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="counter">Operation counter</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{hash}/{counter}")]
        public async Task<ActionResult<IEnumerable<Operation>>> GetByHashCounter(
            [Required][OpHash] string hash,
            [Min(0)] int counter,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("micheline", micheline), ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.Get(hash, counter, micheline, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get operations by hash, counter and nonce
        /// </summary>
        /// <remarks>
        /// Returns an internal operations with the specified hash, counter and nonce.  
        /// **NOTE: if you know in advance what operation type you want to get (e.g. transactions), prefer using `/v1/operations/{type}/{hash}/{counter}/{nonce}`
        /// (e.g. [/v1/operations/transactions/{hash}/{counter}/{nonce}](#operation/Operations_GetTransactionByHashCounterNonce)) instead, because it's much more efficient.**
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="counter">Operation counter</param>
        /// <param name="nonce">Operation nonce (internal)</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{hash}/{counter}/{nonce}")]
        public async Task<ActionResult<IEnumerable<Operation>>> GetByHashCounterNonce(
            [Required][OpHash] string hash,
            [Min(0)] int counter,
            [Min(0)] int nonce,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("micheline", micheline), ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.Get(hash, counter, nonce, micheline, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get operation status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.  
        /// **NOTE: if you know in advance what operation type you want to check (e.g. transactions), prefer using `/v1/operations/{type}/{hash}/status`
        /// (e.g. [/v1/operations/transactions/{hash}/status](#operation/Operations_GetTransactionStatus)) instead, because it's much more efficient.**
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("{hash}/status")]
        public async Task<ActionResult<bool?>> GetStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region endorsements
        /// <summary>
        /// Get endorsements
        /// </summary>
        /// <remarks>
        /// Returns a list of endorsement operations.
        /// </remarks>
        /// <param name="delegate">Filters endorsements by delegate. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters endorsements by level.</param>
        /// <param name="timestamp">Filters endorsements by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts endorsements by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("endorsements")]
        public async Task<ActionResult<IEnumerable<EndorsementOperation>>> GetEndorsements(
            AccountParameter? @delegate,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (@delegate != null)
            {
                if (@delegate.Eqx != null)
                    return new BadRequest($"{nameof(@delegate)}.eqx", "This parameter doesn't support .eqx mode.");

                if (@delegate.Nex != null)
                    return new BadRequest($"{nameof(@delegate)}.nex", "This parameter doesn't support .nex mode.");

                if (@delegate.Eq == -1 || @delegate.In?.Count == 0 || @delegate.Null == true)
                    return Ok(Enumerable.Empty<EndorsementOperation>());
            }

            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("delegate", @delegate), ("level", level), ("timestamp", timestamp),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetEndorsements(null, @delegate, level, timestamp, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetEndorsements(@delegate, level, timestamp, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetEndorsements(@delegate, level, timestamp, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetEndorsements(@delegate, level, timestamp, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetEndorsements(@delegate, level, timestamp, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get endorsement by hash
        /// </summary>
        /// <remarks>
        /// Returns an endorsement operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("endorsements/{hash}")]
        public async Task<ActionResult<IEnumerable<EndorsementOperation>>> GetEndorsementByHash(
            [Required][OpHash] string hash, 
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetEndorsements(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get endorsements count
        /// </summary>
        /// <remarks>
        /// Returns the total number of endorsement operations.
        /// </remarks>
        /// <param name="level">Filters endorsements by level.</param>
        /// <param name="timestamp">Filters endorsements by timestamp.</param>
        /// <returns></returns>
        [HttpGet("endorsements/count")]
        public async Task<ActionResult<int>> GetEndorsementsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.EndorsementOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetEndorsementsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region preendorsements
        /// <summary>
        /// Get preendorsements
        /// </summary>
        /// <remarks>
        /// Returns a list of preendorsement operations.
        /// </remarks>
        /// <param name="delegate">Filters by delegate. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("preendorsements")]
        public async Task<ActionResult<IEnumerable<PreendorsementOperation>>> GetPreendorsements(
            AccountParameter? @delegate,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (@delegate != null)
            {
                if (@delegate.Eqx != null)
                    return new BadRequest($"{nameof(@delegate)}.eqx", "This parameter doesn't support .eqx mode.");

                if (@delegate.Nex != null)
                    return new BadRequest($"{nameof(@delegate)}.nex", "This parameter doesn't support .nex mode.");

                if (@delegate.Eq == -1 || @delegate.In?.Count == 0 || @delegate.Null == true)
                    return Ok(Enumerable.Empty<PreendorsementOperation>());
            }

            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("delegate", @delegate), ("level", level), ("timestamp", timestamp),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetPreendorsements(null, @delegate, level, timestamp, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetPreendorsements(@delegate, level, timestamp, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetPreendorsements(@delegate, level, timestamp, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetPreendorsements(@delegate, level, timestamp, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetPreendorsements(@delegate, level, timestamp, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get preendorsement by hash
        /// </summary>
        /// <remarks>
        /// Returns an preendorsement operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("preendorsements/{hash}")]
        public async Task<ActionResult<IEnumerable<PreendorsementOperation>>> GetPreendorsementByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetPreendorsements(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get preendorsements count
        /// </summary>
        /// <remarks>
        /// Returns the total number of preendorsement operations.
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("preendorsements/count")]
        public async Task<ActionResult<int>> GetPreendorsementsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.PreendorsementOpsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetPreendorsementsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region ballots
        /// <summary>
        /// Get ballots
        /// </summary>
        /// <remarks>
        /// Returns a list of ballot operations.
        /// </remarks>
        /// <param name="delegate">Filters ballots by delegate. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters ballots by level.</param>
        /// <param name="timestamp">Filters ballots by timestamp.</param>
        /// <param name="epoch">Filters ballots by voting epoch.</param>
        /// <param name="period">Filters ballots by voting period.</param>
        /// <param name="proposal">Filters ballots by proposal hash.</param>
        /// <param name="vote">Filters ballots by vote (`yay`, `nay`, `pass`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts ballots by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("ballots")]
        public async Task<ActionResult<IEnumerable<BallotOperation>>> GetBallots(
            AccountParameter? @delegate,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            Int32Parameter? epoch,
            Int32Parameter? period,
            ProtocolParameter? proposal,
            VoteParameter? vote,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (@delegate != null)
            {
                if (@delegate.Eqx != null)
                    return new BadRequest($"{nameof(@delegate)}.eqx", "This parameter doesn't support .eqx mode.");

                if (@delegate.Nex != null)
                    return new BadRequest($"{nameof(@delegate)}.nex", "This parameter doesn't support .nex mode.");

                if (@delegate.Eq == -1 || @delegate.In?.Count == 0 || @delegate.Null == true)
                    return Ok(Enumerable.Empty<BallotOperation>());
            }

            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("delegate", @delegate), ("level", level), ("timestamp", timestamp), ("epoch", epoch),
                ("period", period), ("proposal", proposal), ("vote", vote), ("select", select),
                ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetBallots(null, @delegate, level, timestamp, epoch, period, proposal, vote, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetBallots(@delegate, level, timestamp, epoch, period, proposal, vote, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetBallots(@delegate, level, timestamp, epoch, period, proposal, vote, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetBallots(@delegate, level, timestamp, epoch, period, proposal, vote, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetBallots(@delegate, level, timestamp, epoch, period, proposal, vote, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get ballot by hash
        /// </summary>
        /// <remarks>
        /// Returns a ballot operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("ballots/{hash}")]
        public async Task<ActionResult<IEnumerable<BallotOperation>>> GetBallotByHash(
            [Required][OpHash] string hash, 
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetBallots(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get ballots count
        /// </summary>
        /// <remarks>
        /// Returns the total number of ballot operations.
        /// </remarks>
        /// <param name="level">Filters ballot operations by level.</param>
        /// <param name="timestamp">Filters ballot operations by timestamp.</param>
        /// <returns></returns>
        [HttpGet("ballots/count")]
        public async Task<ActionResult<int>> GetBallotsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.BallotOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetBallotsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region proposals
        /// <summary>
        /// Get proposals
        /// </summary>
        /// <remarks>
        /// Returns a list of proposal operations.
        /// </remarks>
        /// <param name="delegate">Filters proposal operations by delegate. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters proposal operations by level.</param>
        /// <param name="timestamp">Filters proposal operations by timestamp.</param>
        /// <param name="epoch">Filters proposal operations by voting epoch.</param>
        /// <param name="period">Filters proposal operations by voting period.</param>
        /// <param name="proposal">Filters proposal operations by proposal hash.</param>
        /// <param name="duplicated">Specify whether to include or exclude duplicates, which didn't actually upvote a proposal.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts proposal operations by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("proposals")]
        public async Task<ActionResult<IEnumerable<ProposalOperation>>> GetProposals(
            AccountParameter? @delegate,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            Int32Parameter? epoch,
            Int32Parameter? period,
            ProtocolParameter? proposal,
            BoolParameter? duplicated,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (@delegate != null)
            {
                if (@delegate.Eqx != null)
                    return new BadRequest($"{nameof(@delegate)}.eqx", "This parameter doesn't support .eqx mode.");

                if (@delegate.Nex != null)
                    return new BadRequest($"{nameof(@delegate)}.nex", "This parameter doesn't support .nex mode.");

                if (@delegate.Eq == -1 || @delegate.In?.Count == 0 || @delegate.Null == true)
                    return Ok(Enumerable.Empty<ProposalOperation>());
            }

            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("delegate", @delegate), ("level", level), ("timestamp", timestamp), ("epoch", epoch),
                ("period", period), ("proposal", proposal), ("duplicated", duplicated), ("select", select),
                ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetProposals(null, @delegate, level, timestamp, epoch, period, proposal, duplicated, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetProposals(@delegate, level, timestamp, epoch, period, proposal, duplicated, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetProposals(@delegate, level, timestamp, epoch, period, proposal, duplicated, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetProposals(@delegate, level, timestamp, epoch, period, proposal, duplicated, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetProposals(@delegate, level, timestamp, epoch, period, proposal, duplicated, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get proposal by hash
        /// </summary>
        /// <remarks>
        /// Returns a proposal operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("proposals/{hash}")]
        public async Task<ActionResult<IEnumerable<ProposalOperation>>> GetProposalByHash(
            [Required][OpHash] string hash, 
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetProposals(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get proposals count
        /// </summary>
        /// <remarks>
        /// Returns the total number of proposal operations.
        /// </remarks>
        /// <param name="level">Filters proposal operations by level.</param>
        /// <param name="timestamp">Filters proposal operations by timestamp.</param>
        /// <returns></returns>
        [HttpGet("proposals/count")]
        public  async Task<ActionResult<int>> GetProposalsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.ProposalOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetProposalsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region activations
        /// <summary>
        /// Get activations
        /// </summary>
        /// <remarks>
        /// Returns a list of activation operations.
        /// </remarks>
        /// <param name="account">Filters activations by account. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters activations by level.</param>
        /// <param name="timestamp">Filters activations by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts activations by specified field. Supported fields: `id` (default), `level`, `balance`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("activations")]
        public async Task<ActionResult<IEnumerable<ActivationOperation>>> GetActivations(
            AccountParameter? account,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (account != null)
            {
                if (account.Eqx != null)
                    return new BadRequest($"{nameof(account)}.eqx", "This parameter doesn't support .eqx mode.");

                if (account.Nex != null)
                    return new BadRequest($"{nameof(account)}.nex", "This parameter doesn't support .nex mode.");

                if (account.Eq == -1 || account.In?.Count == 0 || account.Null == true)
                    return Ok(Enumerable.Empty<ActivationOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "balance"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("account", account), ("level", level), ("timestamp", timestamp), ("select", select),
                ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetActivations(null, account, level, timestamp, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetActivations(account, level, timestamp, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetActivations(account, level, timestamp, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetActivations(account, level, timestamp, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetActivations(account, level, timestamp, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get activation by hash
        /// </summary>
        /// <remarks>
        /// Returns an activation operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("activations/{hash}")]
        public async Task<ActionResult<IEnumerable<ActivationOperation>>> GetActivationByHash(
            [Required][OpHash] string hash, 
            Symbols quote = Symbols.None)
        {           
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetActivations(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get activations count
        /// </summary>
        /// <remarks>
        /// Returns the total number of activation operations.
        /// </remarks>
        /// <param name="level">Filters activations by level.</param>
        /// <param name="timestamp">Filters activations by timestamp.</param>
        /// <returns></returns>
        [HttpGet("activations/count")]
        public async Task<ActionResult<int>> GetActivationsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.ActivationOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetActivationsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region dal entrapment evidence
        /// <summary>
        /// Get dal entrapment evidences
        /// </summary>
        /// <remarks>
        /// Returns a list of dal entrapment evidence operations.
        /// </remarks>
        /// <param name="anyof">Filters by any of the specified fields. Example: `anyof.accuser.offender=tz1...` will return operations where `accuser` OR `offender` is equal to the specified value. This parameter is useful when you need to retrieve all operations associated with a specified account.</param>
        /// <param name="accuser">Filters by accuser. Allowed fields for `.eqx` mode: `offender`.</param>
        /// <param name="offender">Filters by offender. Allowed fields for `.eqx` mode: `accuser`.</param>
        /// <param name="id">Filters operations by internal TzKT id.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`, `trapLevel`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("dal_entrapment_evidence")]
        public async Task<ActionResult<IEnumerable<DalEntrapmentEvidenceOperation>>> GetDalEntrapmentEvidenceOps(
            [OpenApiExtensionData("x-tzkt-extension", "anyof-parameter")]
            [OpenApiExtensionData("x-tzkt-anyof-parameter", "accuser,offender")]
            AnyOfParameter? anyof,
            AccountParameter? accuser,
            AccountParameter? offender,
            Int64Parameter? id,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "accuser" && x != "offender"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `accuser`, `offender` fields only.");

                if (anyof.Eq == -1 || anyof.In?.Count == 0 || anyof.Null == true)
                    return Ok(Enumerable.Empty<DalEntrapmentEvidenceOperation>());
            }

            if (accuser != null)
            {
                if (accuser.Eqx != null && accuser.Eqx != "offender")
                    return new BadRequest($"{nameof(accuser)}.eqx", "The 'accuser' field can be compared with the 'offender' field only.");

                if (accuser.Nex != null && accuser.Nex != "offender")
                    return new BadRequest($"{nameof(accuser)}.nex", "The 'accuser' field can be compared with the 'offender' field only.");

                if (accuser.Eq == -1 || accuser.In?.Count == 0 || accuser.Null == true)
                    return Ok(Enumerable.Empty<DalEntrapmentEvidenceOperation>());
            }

            if (offender != null)
            {
                if (offender.Eqx != null && offender.Eqx != "accuser")
                    return new BadRequest($"{nameof(offender)}.eqx", "The 'offender' field can be compared with the 'accuser' field only.");

                if (offender.Nex != null && offender.Nex != "accuser")
                    return new BadRequest($"{nameof(offender)}.nex", "The 'offender' field can be compared with the 'accuser' field only.");

                if (offender.Eq == -1 || offender.In?.Count == 0 || offender.Null == true)
                    return Ok(Enumerable.Empty<DalEntrapmentEvidenceOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "trapLevel"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("anyof", anyof), ("accuser", accuser), ("offender", offender), ("id", id), ("level", level), ("timestamp", timestamp),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetDalEntrapmentEvidences(null, anyof, accuser, offender, id, level, timestamp, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetDalEntrapmentEvidences(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetDalEntrapmentEvidences(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetDalEntrapmentEvidences(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetDalEntrapmentEvidences(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get dal entrapment evidence by hash
        /// </summary>
        /// <remarks>
        /// Returns dal entrapment evidence operations with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("dal_entrapment_evidence/{hash}")]
        public async Task<ActionResult<IEnumerable<DalEntrapmentEvidenceOperation>>> GetDalEntrapmentEvidenceOpsByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetDalEntrapmentEvidences(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get dal entrapment evidences count
        /// </summary>
        /// <remarks>
        /// Returns the total number of dal entrapment evidence operations.
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("dal_entrapment_evidence/count")]
        public async Task<ActionResult<int>> GetDalEntrapmentEvidenceOpsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.DalEntrapmentEvidenceOpsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetDalEntrapmentEvidencesCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region double baking
        /// <summary>
        /// Get double baking
        /// </summary>
        /// <remarks>
        /// Returns a list of double baking operations.
        /// </remarks>
        /// <param name="anyof">Filters double baking operations by any of the specified fields. Example: `anyof.accuser.offender=tz1...` will return operations where `accuser` OR `offender` is equal to the specified value. This parameter is useful when you need to retrieve all operations associated with a specified account.</param>
        /// <param name="accuser">Filters double baking operations by accuser. Allowed fields for `.eqx` mode: `offender`.</param>
        /// <param name="offender">Filters double baking operations by offender. Allowed fields for `.eqx` mode: `accuser`.</param>
        /// <param name="id">Filters operations by internal TzKT id.</param>
        /// <param name="level">Filters double baking operations by level.</param>
        /// <param name="timestamp">Filters double baking operations by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts double baking operations by specified field. Supported fields: `id` (default), `level`, `accusedLevel`, `accuserRewards`, `offenderLostDeposits`, `offenderLostRewards`, `offenderLostFees`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("double_baking")]
        public async Task<ActionResult<IEnumerable<DoubleBakingOperation>>> GetDoubleBaking(
            [OpenApiExtensionData("x-tzkt-extension", "anyof-parameter")]
            [OpenApiExtensionData("x-tzkt-anyof-parameter", "accuser,offender")]
            AnyOfParameter? anyof,
            AccountParameter? accuser,
            AccountParameter? offender,
            Int64Parameter? id,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "accuser" && x != "offender"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `accuser`, `offender` fields only.");

                if (anyof.Eq == -1 || anyof.In?.Count == 0 || anyof.Null == true)
                    return Ok(Enumerable.Empty<DoubleBakingOperation>());
            }

            if (accuser != null)
            {
                if (accuser.Eqx != null && accuser.Eqx != "offender")
                    return new BadRequest($"{nameof(accuser)}.eqx", "The 'accuser' field can be compared with the 'offender' field only.");

                if (accuser.Nex != null && accuser.Nex != "offender")
                    return new BadRequest($"{nameof(accuser)}.nex", "The 'accuser' field can be compared with the 'offender' field only.");

                if (accuser.Eq == -1 || accuser.In?.Count == 0 || accuser.Null == true)
                    return Ok(Enumerable.Empty<DoubleBakingOperation>());
            }

            if (offender != null)
            {
                if (offender.Eqx != null && offender.Eqx != "accuser")
                    return new BadRequest($"{nameof(offender)}.eqx", "The 'offender' field can be compared with the 'accuser' field only.");

                if (offender.Nex != null && offender.Nex != "accuser")
                    return new BadRequest($"{nameof(offender)}.nex", "The 'offender' field can be compared with the 'accuser' field only.");

                if (offender.Eq == -1 || offender.In?.Count == 0 || offender.Null == true)
                    return Ok(Enumerable.Empty<DoubleBakingOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "accusedLevel", "accuserRewards", "offenderLostDeposits", "offenderLostRewards", "offenderLostFees"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("anyof", anyof), ("accuser", accuser), ("offender", offender), ("id", id), ("level", level), ("timestamp", timestamp),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetDoubleBakings(null, anyof, accuser, offender, id, level, timestamp, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetDoubleBakings(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetDoubleBakings(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetDoubleBakings(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetDoubleBakings(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get double baking by hash
        /// </summary>
        /// <remarks>
        /// Returns a double baking operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("double_baking/{hash}")]
        public async Task<ActionResult<IEnumerable<DoubleBakingOperation>>> GetDoubleBakingByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetDoubleBakings(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get double baking count
        /// </summary>
        /// <remarks>
        /// Returns the total number of double baking operations.
        /// </remarks>
        /// <param name="level">Filters double baking operations by level.</param>
        /// <param name="timestamp">Filters double baking operations by timestamp.</param>
        /// <returns></returns>
        [HttpGet("double_baking/count")]
        public async Task<ActionResult<int>> GetDoubleBakingCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.DoubleBakingOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetDoubleBakingsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region double endorsing
        /// <summary>
        /// Get double endorsing
        /// </summary>
        /// <remarks>
        /// Returns a list of double endorsing operations.
        /// </remarks>
        /// <param name="anyof">Filters double endorsing operations by any of the specified fields. Example: `anyof.accuser.offender=tz1...` will return operations where `accuser` OR `offender` is equal to the specified value. This parameter is useful when you need to retrieve all operations associated with a specified account.</param>
        /// <param name="accuser">Filters double endorsing operations by accuser. Allowed fields for `.eqx` mode: `offender`.</param>
        /// <param name="offender">Filters double endorsing operations by offender. Allowed fields for `.eqx` mode: `accuser`.</param>
        /// <param name="id">Filters operations by internal TzKT id.</param>
        /// <param name="level">Filters double endorsing operations by level.</param>
        /// <param name="timestamp">Filters double endorsing operations by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts double endorsing operations by specified field. Supported fields: `id` (default), `level`, `accusedLevel`, `accuserRewards`, `offenderLostDeposits`, `offenderLostRewards`, `offenderLostFees`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("double_endorsing")]
        public async Task<ActionResult<IEnumerable<DoubleEndorsingOperation>>> GetDoubleEndorsing(
            [OpenApiExtensionData("x-tzkt-extension", "anyof-parameter")]
            [OpenApiExtensionData("x-tzkt-anyof-parameter", "accuser,offender")]
            AnyOfParameter? anyof,
            AccountParameter? accuser,
            AccountParameter? offender,
            Int64Parameter? id,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "accuser" && x != "offender"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `accuser`, `offender` fields only.");

                if (anyof.Eq == -1 || anyof.In?.Count == 0 || anyof.Null == true)
                    return Ok(Enumerable.Empty<DoubleEndorsingOperation>());
            }

            if (accuser != null)
            {
                if (accuser.Eqx != null && accuser.Eqx != "offender")
                    return new BadRequest($"{nameof(accuser)}.eqx", "The 'accuser' field can be compared with the 'offender' field only.");

                if (accuser.Nex != null && accuser.Nex != "offender")
                    return new BadRequest($"{nameof(accuser)}.nex", "The 'accuser' field can be compared with the 'offender' field only.");

                if (accuser.Eq == -1 || accuser.In?.Count == 0 || accuser.Null == true)
                    return Ok(Enumerable.Empty<DoubleEndorsingOperation>());
            }

            if (offender != null)
            {
                if (offender.Eqx != null && offender.Eqx != "accuser")
                    return new BadRequest($"{nameof(offender)}.eqx", "The 'offender' field can be compared with the 'accuser' field only.");

                if (offender.Nex != null && offender.Nex != "accuser")
                    return new BadRequest($"{nameof(offender)}.nex", "The 'offender' field can be compared with the 'accuser' field only.");

                if (offender.Eq == -1 || offender.In?.Count == 0 || offender.Null == true)
                    return Ok(Enumerable.Empty<DoubleEndorsingOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "accusedLevel", "accuserRewards", "offenderLostDeposits", "offenderLostRewards", "offenderLostFees"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("anyof", anyof), ("accuser", accuser), ("offender", offender), ("id", id), ("level", level), ("timestamp", timestamp),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetDoubleEndorsings(null, anyof, accuser, offender, id, level, timestamp, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetDoubleEndorsings(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetDoubleEndorsings(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetDoubleEndorsings(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetDoubleEndorsings(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get double endorsing by hash
        /// </summary>
        /// <remarks>
        /// Returns a double endorsing operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("double_endorsing/{hash}")]
        public async Task<ActionResult<IEnumerable<DoubleEndorsingOperation>>> GetDoubleEndorsingByHash(
            [Required][OpHash] string hash, 
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetDoubleEndorsings(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get double endorsing count
        /// </summary>
        /// <remarks>
        /// Returns the total number of double endorsing operations.
        /// </remarks>
        /// <param name="level">Filters double endorsing operations by level.</param>
        /// <param name="timestamp">Filters double endorsing operations by timestamp.</param>
        /// <returns></returns>
        [HttpGet("double_endorsing/count")]
        public async Task<ActionResult<int>> GetDoubleEndorsingCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.DoubleEndorsingOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetDoubleEndorsingsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region double preendorsing
        /// <summary>
        /// Get double preendorsing
        /// </summary>
        /// <remarks>
        /// Returns a list of double preendorsing operations.
        /// </remarks>
        /// <param name="anyof">Filters by any of the specified fields. Example: `anyof.accuser.offender=tz1...` will return operations where `accuser` OR `offender` is equal to the specified value. This parameter is useful when you need to retrieve all operations associated with a specified account.</param>
        /// <param name="accuser">Filters by accuser. Allowed fields for `.eqx` mode: `offender`.</param>
        /// <param name="offender">Filters by offender. Allowed fields for `.eqx` mode: `accuser`.</param>
        /// <param name="id">Filters operations by internal TzKT id.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`, `accusedLevel`, `accuserRewards`, `offenderLostDeposits`, `offenderLostRewards`, `offenderLostFees`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("double_preendorsing")]
        public async Task<ActionResult<IEnumerable<DoublePreendorsingOperation>>> GetDoublePreendorsing(
            [OpenApiExtensionData("x-tzkt-extension", "anyof-parameter")]
            [OpenApiExtensionData("x-tzkt-anyof-parameter", "accuser,offender")]
            AnyOfParameter? anyof,
            AccountParameter? accuser,
            AccountParameter? offender,
            Int64Parameter? id,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "accuser" && x != "offender"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `accuser`, `offender` fields only.");

                if (anyof.Eq == -1 || anyof.In?.Count == 0 || anyof.Null == true)
                    return Ok(Enumerable.Empty<DoublePreendorsingOperation>());
            }

            if (accuser != null)
            {
                if (accuser.Eqx != null && accuser.Eqx != "offender")
                    return new BadRequest($"{nameof(accuser)}.eqx", "The 'accuser' field can be compared with the 'offender' field only.");

                if (accuser.Nex != null && accuser.Nex != "offender")
                    return new BadRequest($"{nameof(accuser)}.nex", "The 'accuser' field can be compared with the 'offender' field only.");

                if (accuser.Eq == -1 || accuser.In?.Count == 0 || accuser.Null == true)
                    return Ok(Enumerable.Empty<DoublePreendorsingOperation>());
            }

            if (offender != null)
            {
                if (offender.Eqx != null && offender.Eqx != "accuser")
                    return new BadRequest($"{nameof(offender)}.eqx", "The 'offender' field can be compared with the 'accuser' field only.");

                if (offender.Nex != null && offender.Nex != "accuser")
                    return new BadRequest($"{nameof(offender)}.nex", "The 'offender' field can be compared with the 'accuser' field only.");

                if (offender.Eq == -1 || offender.In?.Count == 0 || offender.Null == true)
                    return Ok(Enumerable.Empty<DoublePreendorsingOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "accusedLevel", "accuserRewards", "offenderLostDeposits", "offenderLostRewards", "offenderLostFees"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("anyof", anyof), ("accuser", accuser), ("offender", offender), ("id", id), ("level", level), ("timestamp", timestamp),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetDoublePreendorsings(null, anyof, accuser, offender, id, level, timestamp, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetDoublePreendorsings(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetDoublePreendorsings(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetDoublePreendorsings(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetDoublePreendorsings(anyof, accuser, offender, id, level, timestamp, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get double preendorsing by hash
        /// </summary>
        /// <remarks>
        /// Returns a double preendorsing operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("double_preendorsing/{hash}")]
        public async Task<ActionResult<IEnumerable<DoublePreendorsingOperation>>> GetDoublePreendorsingByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetDoublePreendorsings(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get double preendorsing count
        /// </summary>
        /// <remarks>
        /// Returns the total number of double preendorsing operations.
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("double_preendorsing/count")]
        public async Task<ActionResult<int>> GetDoublePreendorsingCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.DoublePreendorsingOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetDoublePreendorsingsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region nonce revelations
        /// <summary>
        /// Get nonce revelations
        /// </summary>
        /// <remarks>
        /// Returns a list of seed nonce revelation operations.
        /// </remarks>
        /// <param name="anyof">Filters nonce revelation operations by any of the specified fields. Example: `anyof.baker.sender=tz1...` will return operations where `baker` OR `sender` is equal to the specified value. This parameter is useful when you need to retrieve all operations associated with a specified account.</param>
        /// <param name="baker">Filters nonce revelation operations by baker. Allowed fields for `.eqx` mode: `sender`.</param>
        /// <param name="sender">Filters nonce revelation operations by sender. Allowed fields for `.eqx` mode: `baker`.</param>
        /// <param name="level">Filters nonce revelation operations by level.</param>
        /// <param name="revealedCycle">Filters by cycle for which the nonce was revealed.</param>
        /// <param name="timestamp">Filters nonce revelation operations by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts nonce revelation operations by specified field. Supported fields: `id` (default), `level`, `revealedLevel`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("nonce_revelations")]
        public async Task<ActionResult<IEnumerable<NonceRevelationOperation>>> GetNonceRevelations(
            [OpenApiExtensionData("x-tzkt-extension", "anyof-parameter")]
            [OpenApiExtensionData("x-tzkt-anyof-parameter", "baker,sender")]
            AnyOfParameter? anyof,
            AccountParameter? baker,
            AccountParameter? sender,
            Int32Parameter? level,
            Int32Parameter? revealedCycle,
            TimestampParameter? timestamp,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "baker" && x != "sender"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `baker`, `sender` fields only.");

                if (anyof.Eq == -1 || anyof.In?.Count == 0 || anyof.Null == true)
                    return Ok(Enumerable.Empty<NonceRevelationOperation>());
            }

            if (baker != null)
            {
                if (baker.Eqx != null && baker.Eqx != "sender")
                    return new BadRequest($"{nameof(baker)}.eqx", "The 'baker' field can be compared with the 'sender' field only.");

                if (baker.Nex != null && baker.Nex != "sender")
                    return new BadRequest($"{nameof(baker)}.nex", "The 'baker' field can be compared with the 'sender' field only.");

                if (baker.Eq == -1 || baker.In?.Count == 0 || baker.Null == true)
                    return Ok(Enumerable.Empty<NonceRevelationOperation>());
            }

            if (sender != null)
            {
                if (sender.Eqx != null && sender.Eqx != "baker")
                    return new BadRequest($"{nameof(sender)}.eqx", "The 'sender' field can be compared with the 'baker' field only.");

                if (sender.Nex != null && sender.Nex != "baker")
                    return new BadRequest($"{nameof(sender)}.nex", "The 'sender' field can be compared with the 'baker' field only.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<NonceRevelationOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "revealedLevel"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("anyof", anyof), ("baker", baker), ("sender", sender), ("level", level), ("revealedCycle", revealedCycle),
                ("timestamp", timestamp), ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetNonceRevelations(null, anyof, baker, sender, level, revealedCycle, timestamp, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetNonceRevelations(anyof, baker, sender, level, revealedCycle, timestamp, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetNonceRevelations(anyof, baker, sender, level, revealedCycle, timestamp, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetNonceRevelations(anyof, baker, sender, level, revealedCycle, timestamp, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetNonceRevelations(anyof, baker, sender, level, revealedCycle, timestamp, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get nonce revelation by hash
        /// </summary>
        /// <remarks>
        /// Returns a seed nonce revelation operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("nonce_revelations/{hash}")]
        public async Task<ActionResult<IEnumerable<NonceRevelationOperation>>> GetNonceRevelationByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetNonceRevelations(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get nonce revelations count
        /// </summary>
        /// <remarks>
        /// Returns the total number of seed nonce revelation operations.
        /// </remarks>
        /// <param name="level">Filters seed nonce revelation operations by level.</param>
        /// <param name="timestamp">Filters seed nonce revelation operations by timestamp.</param>
        /// <returns></returns>
        [HttpGet("nonce_revelations/count")]
        public async Task<ActionResult<int>> GetNonceRevelationsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.NonceRevelationOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetNonceRevelationsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region vdf revelations
        /// <summary>
        /// Get vdf revelations
        /// </summary>
        /// <remarks>
        /// Returns a list of vdf revelation operations.
        /// </remarks>
        /// <param name="baker">Filters by baker. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="cycle">Filters by cycle in which the operation was included.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("vdf_revelations")]
        public async Task<ActionResult<IEnumerable<VdfRevelationOperation>>> GetVdfRevelations(
            AccountParameter? baker,
            Int32Parameter? level,
            Int32Parameter? cycle,
            TimestampParameter? timestamp,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (baker != null)
            {
                if (baker.Eqx != null)
                    return new BadRequest($"{nameof(baker)}.eqx", "This parameter doesn't support .eqx mode.");

                if (baker.Nex != null)
                    return new BadRequest($"{nameof(baker)}.nex", "This parameter doesn't support .nex mode.");

                if (baker.Eq == -1 || baker.In?.Count == 0 || baker.Null == true)
                    return Ok(Enumerable.Empty<VdfRevelationOperation>());
            }

            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("baker", baker), ("level", level), ("cycle", cycle), ("timestamp", timestamp),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetVdfRevelations(null, baker, level, cycle, timestamp, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetVdfRevelations(baker, level, cycle, timestamp, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetVdfRevelations(baker, level, cycle, timestamp, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetVdfRevelations(baker, level, cycle, timestamp, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetVdfRevelations(baker, level, cycle, timestamp, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get vdf revelation by hash
        /// </summary>
        /// <remarks>
        /// Returns a vdf revelation operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("vdf_revelations/{hash}")]
        public async Task<ActionResult<IEnumerable<VdfRevelationOperation>>> GetVdfRevelationByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetVdfRevelations(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get vdf revelations count
        /// </summary>
        /// <remarks>
        /// Returns the total number of vdf revelation operations.
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("vdf_revelations/count")]
        public async Task<ActionResult<int>> GetVdfRevelationsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.VdfRevelationOpsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetVdfRevelationsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region drain delegate
        /// <summary>
        /// Get drain delegate
        /// </summary>
        /// <remarks>
        /// Returns a list of drain delegate operations.
        /// </remarks>
        /// <param name="anyof">Filters by any of the specified fields. Example: `anyof.delegate.target=tz1...` will return operations where `delegate` OR `target` is equal to the specified value. This parameter is useful when you need to retrieve all operations associated with a specified account.</param>
        /// <param name="delegate">Filters by drained baker. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="target">Filters by target. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("drain_delegate")]
        public async Task<ActionResult<IEnumerable<DrainDelegateOperation>>> GetDrainDelegateOps(
            [OpenApiExtensionData("x-tzkt-extension", "anyof-parameter")]
            [OpenApiExtensionData("x-tzkt-anyof-parameter", "delegate,target")]
            AnyOfParameter? anyof,
            AccountParameter? @delegate,
            AccountParameter? target,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (@delegate != null)
            {
                if (@delegate.Eqx != null)
                    return new BadRequest($"{nameof(@delegate)}.eqx", "This parameter doesn't support .eqx mode.");

                if (@delegate.Nex != null)
                    return new BadRequest($"{nameof(@delegate)}.nex", "This parameter doesn't support .nex mode.");

                if (@delegate.Eq == -1 || @delegate.In?.Count == 0 || @delegate.Null == true)
                    return Ok(Enumerable.Empty<DrainDelegateOperation>());
            }

            if (target != null)
            {
                if (target.Eqx != null)
                    return new BadRequest($"{nameof(target)}.eqx", "This parameter doesn't support .eqx mode.");

                if (target.Nex != null)
                    return new BadRequest($"{nameof(target)}.nex", "This parameter doesn't support .nex mode.");

                if (target.Eq == -1 || target.In?.Count == 0 || target.Null == true)
                    return Ok(Enumerable.Empty<DrainDelegateOperation>());
            }

            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("anyof", anyof), ("delegate", @delegate), ("target", target), ("level", level), ("timestamp", timestamp),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetDrainDelegates(null, anyof, @delegate, target, level, timestamp, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetDrainDelegates(anyof, @delegate, target, level, timestamp, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetDrainDelegates(anyof, @delegate, target, level, timestamp, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetDrainDelegates(anyof, @delegate, target, level, timestamp, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetDrainDelegates(anyof, @delegate, target, level, timestamp, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get drain delegate by hash
        /// </summary>
        /// <remarks>
        /// Returns a drain delegate operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("drain_delegate/{hash}")]
        public async Task<ActionResult<IEnumerable<DrainDelegateOperation>>> GetDrainDelegateByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetDrainDelegates(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get drain delegate count
        /// </summary>
        /// <remarks>
        /// Returns the total number of drain delegate operations.
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("drain_delegate/count")]
        public async Task<ActionResult<int>> GetDrainDelegateOpsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.DrainDelegateOpsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetDrainDelegatesCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region delegations
        /// <summary>
        /// Get delegations
        /// </summary>
        /// <remarks>
        /// Returns a list of delegation operations.
        /// </remarks>
        /// <param name="anyof">Filters delegations by any of the specified fields. Example: `anyof.prevDelegate.newDelegate=tz1...` will return operations where `prevDelegate` OR `newDelegate` is equal to the specified value. This parameter is useful when you need to retrieve all delegations associated with a specified account.</param>
        /// <param name="initiator">Filters delegations by initiator. Allowed fields for `.eqx` mode: `prevDelegate`, `newDelegate`.</param>
        /// <param name="sender">Filters delegations by sender. Allowed fields for `.eqx` mode: `prevDelegate`, `newDelegate`.</param>
        /// <param name="prevDelegate">Filters delegations by prev delegate. Allowed fields for `.eqx` mode: `initiator`, `sender`, `newDelegate`.</param>
        /// <param name="newDelegate">Filters delegations by new delegate. Allowed fields for `.eqx` mode: `initiator`, `sender`, `prevDelegate`.</param>
        /// <param name="id">Filters operations by internal TzKT id.</param>
        /// <param name="level">Filters delegations by level.</param>
        /// <param name="timestamp">Filters delegations by timestamp.</param>
        /// <param name="senderCodeHash">Filters by `senderCodeHash`.</param>
        /// <param name="status">Filters delegations by operation status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts delegations by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `bakerFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("delegations")]
        public async Task<ActionResult<IEnumerable<DelegationOperation>>> GetDelegations(
            [OpenApiExtensionData("x-tzkt-extension", "anyof-parameter")]
            [OpenApiExtensionData("x-tzkt-anyof-parameter", "sender,prevDelegate,newDelegate")]
            AnyOfParameter? anyof,
            AccountParameter? initiator,
            AccountParameter? sender,
            AccountParameter? prevDelegate,
            AccountParameter? newDelegate,
            Int64Parameter? id,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            Int32Parameter? senderCodeHash,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "initiator" && x != "sender" && x != "prevDelegate" && x != "newDelegate"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `initiator`, `sender`, `prevDelegate`, `newDelegate` fields only.");

                if (anyof.Eq == -1 || anyof.In?.Count == 0 && !anyof.InHasNull)
                    return Ok(Enumerable.Empty<DelegationOperation>());
            }

            if (initiator != null)
            {
                if (initiator.Eqx != null && initiator.Eqx != "prevDelegate" && initiator.Eqx != "newDelegate")
                    return new BadRequest($"{nameof(initiator)}.eqx", "The 'initiator' field can be compared with the 'prevDelegate' or 'newDelegate' field only.");

                if (initiator.Nex != null && initiator.Nex != "prevDelegate" && initiator.Nex != "newDelegate")
                    return new BadRequest($"{nameof(initiator)}.nex", "The 'initiator' field can be compared with the 'prevDelegate' or 'newDelegate' field only.");

                if (initiator.Eq == -1 || initiator.In?.Count == 0 && !initiator.InHasNull)
                    return Ok(Enumerable.Empty<DelegationOperation>());
            }

            if (sender != null)
            {
                if (sender.Eqx != null && sender.Eqx != "prevDelegate" && sender.Eqx != "newDelegate")
                    return new BadRequest($"{nameof(sender)}.eqx", "The 'sender' field can be compared with the 'prevDelegate' or 'newDelegate' field only.");

                if (sender.Nex != null && sender.Nex != "prevDelegate" && sender.Nex != "newDelegate")
                    return new BadRequest($"{nameof(sender)}.nex", "The 'sender' field can be compared with the 'prevDelegate' or 'newDelegate' field only.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<DelegationOperation>());
            }

            if (prevDelegate != null)
            {
                if (prevDelegate.Eqx != null && prevDelegate.Eqx != "initiator" && prevDelegate.Eqx != "sender" && prevDelegate.Eqx != "newDelegate")
                    return new BadRequest($"{nameof(prevDelegate)}.eqx", "The 'prevDelegate' field can be compared with the 'initiator', 'sender' or 'newDelegate' field only.");

                if (prevDelegate.Nex != null && prevDelegate.Nex != "initiator" && prevDelegate.Nex != "sender" && prevDelegate.Nex != "newDelegate")
                    return new BadRequest($"{nameof(prevDelegate)}.nex", "The 'prevDelegate' field can be compared with the 'initiator', 'sender' or 'newDelegate' field only.");

                if (prevDelegate.Eq == -1 || prevDelegate.In?.Count == 0 && !prevDelegate.InHasNull)
                    return Ok(Enumerable.Empty<DelegationOperation>());
            }

            if (newDelegate != null)
            {
                if (newDelegate.Eqx != null && newDelegate.Eqx != "initiator" && newDelegate.Eqx != "sender" && newDelegate.Eqx != "prevDelegate")
                    return new BadRequest($"{nameof(newDelegate)}.eqx", "The 'newDelegate' field can be compared with the 'initiator', 'sender' or 'prevDelegate' fields only.");

                if (newDelegate.Nex != null && newDelegate.Nex != "initiator" && newDelegate.Nex != "sender" && newDelegate.Nex != "prevDelegate")
                    return new BadRequest($"{nameof(newDelegate)}.nex", "The 'newDelegate' field can be compared with the 'initiator', 'sender' or 'prevDelegate' fields only.");

                if (newDelegate.Eq == -1 || newDelegate.In?.Count == 0 && !newDelegate.InHasNull)
                    return Ok(Enumerable.Empty<DelegationOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "bakerFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("anyof", anyof), ("initiator", initiator), ("sender", sender), ("prevDelegate", prevDelegate), 
                ("newDelegate", newDelegate), ("id", id), ("level", level), ("timestamp", timestamp), ("senderCodeHash", senderCodeHash),
                ("status", status), ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetDelegations(null, anyof, initiator, sender, prevDelegate, newDelegate, id, level, timestamp, senderCodeHash, status, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetDelegations(anyof, initiator, sender, prevDelegate, newDelegate, id, level, timestamp, senderCodeHash, status, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetDelegations(anyof, initiator, sender, prevDelegate, newDelegate, id, level, timestamp, senderCodeHash, status, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetDelegations(anyof, initiator, sender, prevDelegate, newDelegate, id, level, timestamp, senderCodeHash, status, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetDelegations(anyof, initiator, sender, prevDelegate, newDelegate, id, level, timestamp, senderCodeHash, status, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get delegation by hash
        /// </summary>
        /// <remarks>
        /// Returns a delegation operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("delegations/{hash}")]
        public async Task<ActionResult<IEnumerable<DelegationOperation>>> GetDelegationByHash([Required][OpHash] string hash, Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetDelegations(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get delegation status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("delegations/{hash}/status")]
        public async Task<ActionResult<bool?>> GetDelegationStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetDelegationStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get delegations count
        /// </summary>
        /// <remarks>
        /// Returns the total number of delegation operations.
        /// </remarks>
        /// <param name="level">Filters delegations by level.</param>
        /// <param name="timestamp">Filters delegations by timestamp.</param>
        /// <returns></returns>
        [HttpGet("delegations/count")]
        public async Task<ActionResult<int>> GetDelegationsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.DelegationOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetDelegationsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region originations
        /// <summary>
        /// Get originations
        /// </summary>
        /// <remarks>
        /// Returns a list of origination operations.
        /// </remarks>
        /// <param name="anyof">Filters originations by any of the specified fields. Example: `anyof.sender.initiator=tz1...` will return operations where `sender` OR `initiator` is equal to the specified value. This parameter is useful when you need to retrieve all originations associated with a specified account.</param>
        /// <param name="initiator">Filters origination operations by initiator. Allowed fields for `.eqx` mode: `contractDelegate`.</param>
        /// <param name="sender">Filters origination operations by sender. Allowed fields for `.eqx` mode: `contractDelegate`.</param>
        /// <param name="contractDelegate">Filters origination operations by delegate. Allowed fields for `.eqx` mode: `initiator`, `sender`.</param>
        /// <param name="originatedContract">Filters origination operations by originated contract. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="id">Filters origination operations by internal TzKT id</param>
        /// <param name="typeHash">Filters origination operations by 32-bit hash of originated contract parameter and storage types (helpful for searching originations of similar contracts)</param>
        /// <param name="codeHash">Filters origination operations by 32-bit hash of originated contract code (helpful for searching originations of same contracts)</param>
        /// <param name="level">Filters origination operations by level.</param>
        /// <param name="timestamp">Filters origination operations by timestamp.</param>
        /// <param name="senderCodeHash">Filters by `senderCodeHash`.</param>
        /// <param name="anyCodeHash">Filters by either `senderCodeHash` or `codeHash`.</param>
        /// <param name="status">Filters origination operations by operation status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts originations by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `storageUsed`, `bakerFee`, `storageFee`, `allocationFee`, `contractBalance`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("originations")]
        public async Task<ActionResult<IEnumerable<OriginationOperation>>> GetOriginations(
            [OpenApiExtensionData("x-tzkt-extension", "anyof-parameter")]
            [OpenApiExtensionData("x-tzkt-anyof-parameter", "initiator,sender,contractDelegate,originatedContract")]
            AnyOfParameter? anyof,
            AccountParameter? initiator,
            AccountParameter? sender,
            AccountParameter? contractDelegate,
            AccountParameter? originatedContract,
            Int64Parameter? id,
            Int32Parameter? typeHash,
            Int32Parameter? codeHash,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            Int32Parameter? senderCodeHash,
            Int32Parameter? anyCodeHash,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            #region validates
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "initiator" && x != "sender" && x != "contractDelegate" && x != "originatedContract"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `initiator`, `sender`, `contractDelegate`, `originatedContract` fields only.");

                if (anyof.Eq == -1 || anyof.In?.Count == 0 && !anyof.InHasNull)
                    return Ok(Enumerable.Empty<OriginationOperation>());
            }

            if (initiator != null)
            {
                if (initiator.Eqx != null && initiator.Eqx != "contractDelegate")
                    return new BadRequest($"{nameof(initiator)}.eqx", "The 'initiator' field can be compared with the 'contractDelegate' field only.");

                if (initiator.Nex != null && initiator.Nex != "contractDelegate")
                    return new BadRequest($"{nameof(initiator)}.nex", "The 'initiator' field can be compared with the 'contractDelegate' field only.");

                if (initiator.Eq == -1 || initiator.In?.Count == 0 && !initiator.InHasNull)
                    return Ok(Enumerable.Empty<OriginationOperation>());
            }

            if (sender != null)
            {
                if (sender.Eqx != null && sender.Eqx != "contractDelegate")
                    return new BadRequest($"{nameof(sender)}.eqx", "The 'sender' field can be compared with the 'contractDelegate' field only.");

                if (sender.Nex != null && sender.Nex != "contractDelegate")
                    return new BadRequest($"{nameof(sender)}.nex", "The 'sender' field can be compared with the 'contractDelegate' field only.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<OriginationOperation>());
            }

            if (contractDelegate != null)
            {
                if (contractDelegate.Eqx != null && contractDelegate.Eqx != "initiator" && contractDelegate.Eqx != "sender")
                    return new BadRequest($"{nameof(contractDelegate)}.eqx", "The 'contractDelegate' field can be compared with the 'initiator' or 'sender' field only.");

                if (contractDelegate.Nex != null && contractDelegate.Nex != "initiator" && contractDelegate.Nex != "sender")
                    return new BadRequest($"{nameof(contractDelegate)}.nex", "The 'contractDelegate' field can be compared with the 'initiator' or 'sender' field only.");

                if (contractDelegate.Eq == -1 || contractDelegate.In?.Count == 0 && !contractDelegate.InHasNull)
                    return Ok(Enumerable.Empty<OriginationOperation>());
            }

            if (originatedContract != null)
            {
                if (originatedContract.Eqx != null)
                    return new BadRequest($"{nameof(originatedContract)}.eqx", "This parameter doesn't support .eqx mode.");

                if (originatedContract.Nex != null)
                    return new BadRequest($"{nameof(originatedContract)}.nex", "This parameter doesn't support .nex mode.");

                if (originatedContract.Eq == -1 || originatedContract.In?.Count == 0 && !originatedContract.InHasNull)
                    return Ok(Enumerable.Empty<OriginationOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "storageUsed", "bakerFee", "storageFee", "allocationFee", "contractBalance"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("anyof", anyof), ("initiator", initiator), ("sender", sender), 
                ("contractDelegate", contractDelegate), ("originatedContract", originatedContract), ("id", id),  ("typeHash", typeHash),
                ("codeHash", codeHash), ("level", level), ("timestamp", timestamp), ("senderCodeHash", senderCodeHash), ("anyCodeHash", anyCodeHash),
                ("status", status), ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("micheline", micheline), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetOriginations(null, anyof, initiator, sender, contractDelegate, originatedContract, id, typeHash, codeHash, level, timestamp, anyCodeHash, senderCodeHash, status, sort, offset, limit, micheline, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetOriginations(anyof, initiator, sender, contractDelegate, originatedContract, id, typeHash, codeHash, level, timestamp, anyCodeHash, senderCodeHash, status, sort, offset, limit, select.Values[0], micheline, quote);
                else
                    res = await Operations.GetOriginations(anyof, initiator, sender, contractDelegate, originatedContract, id, typeHash, codeHash, level, timestamp, anyCodeHash, senderCodeHash, status, sort, offset, limit, select.Values, micheline, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetOriginations(anyof, initiator, sender, contractDelegate, originatedContract, id, typeHash, codeHash, level, timestamp, anyCodeHash, senderCodeHash, status, sort, offset, limit, select.Fields[0], micheline, quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetOriginations(anyof, initiator, sender, contractDelegate, originatedContract, id, typeHash, codeHash, level, timestamp, anyCodeHash, senderCodeHash, status, sort, offset, limit, select.Fields, micheline, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get origination by hash
        /// </summary>
        /// <remarks>
        /// Returns origination operations with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("originations/{hash}")]
        public async Task<ActionResult<IEnumerable<OriginationOperation>>> GetOriginationByHash(
            [Required][OpHash] string hash,
            MichelineFormat micheline,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("micheline", micheline), ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetOriginations(hash, micheline, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get origination status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("originations/{hash}/status")]
        public async Task<ActionResult<bool?>> GetOriginationStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetOriginationStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get originations count
        /// </summary>
        /// <remarks>
        /// Returns the total number of origination operations.
        /// </remarks>
        /// <param name="level">Filters originations by level.</param>
        /// <param name="timestamp">Filters originations by timestamp.</param>
        /// <returns></returns>
        [HttpGet("originations/count")]
        public async Task<ActionResult<int>> GetOriginationsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.OriginationOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetOriginationsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region transactions
        /// <summary>
        /// Get transactions
        /// </summary>
        /// <remarks>
        /// Returns a list of transaction operations.
        /// </remarks>
        /// <param name="anyof">Filters transactions by any of the specified fields. Example: `anyof.sender.target=tz1...` will return operations where `sender` OR `target` is equal to the specified value. This parameter is useful when you need to retrieve all transactions associated with a specified account.</param>
        /// <param name="initiator">Filters transactions by initiator. Allowed fields for `.eqx` mode: `target`.</param>
        /// <param name="sender">Filters transactions by sender. Allowed fields for `.eqx` mode: `target`.</param>
        /// <param name="target">Filters transactions by target. Allowed fields for `.eqx` mode: `sender`, `initiator`.</param>
        /// <param name="amount">Filters transactions by amount (micro tez).</param>
        /// <param name="id">Filters transactions by id.</param>
        /// <param name="level">Filters transactions by level.</param>
        /// <param name="timestamp">Filters transactions by timestamp.</param>
        /// <param name="senderCodeHash">Filters by `senderCodeHash`.</param>
        /// <param name="targetCodeHash">Filters by `targetCodeHash`.</param>
        /// <param name="codeHash">Filters by either `senderCodeHash` or `targetCodeHash`.</param>
        /// <param name="hasInternals">Filters transactions by presence of internal operations.</param>
        /// <param name="entrypoint">Filters transactions by entrypoint called on the target contract.</param>
        /// <param name="parameter">Filters transactions by parameter value. Note, this query parameter supports the following format: `?parameter{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?parameter.token_id=...` or `?parameter.sigs.0.ne=...`.
        /// Also, note that `.value` part must be omitted in the path, so, for example, filtering by `parameter.value.foo` must be specified as `?parameter.foo=...`.</param>
        /// <param name="status">Filters transactions by operation status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts transactions by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `storageUsed`, `bakerFee`, `storageFee`, `allocationFee`, `amount`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("transactions")]
        public async Task<ActionResult<IEnumerable<TransactionOperation>>> GetTransactions(
            [OpenApiExtensionData("x-tzkt-extension", "anyof-parameter")]
            [OpenApiExtensionData("x-tzkt-anyof-parameter", "sender,target,initiator")]
            AnyOfParameter? anyof,
            AccountParameter? initiator,
            AccountParameter? sender,
            AccountParameter? target,
            Int64Parameter? amount,
            Int64Parameter? id,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            Int32Parameter? senderCodeHash,
            Int32Parameter? targetCodeHash,
            Int32Parameter? codeHash,
            StringParameter? entrypoint,
            JsonParameter? parameter,
            BoolParameter? hasInternals,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None) 
        {
            #region validate
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "initiator" && x != "sender" && x != "target"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `initiator`, `sender`, `target` fields only.");

                if (anyof.Eq == -1 || anyof.In?.Count == 0 && !anyof.InHasNull)
                    return Ok(Enumerable.Empty<TransactionOperation>());
            }

            if (initiator != null)
            {
                if (initiator.Eqx != null && initiator.Eqx != "target")
                    return new BadRequest($"{nameof(initiator)}.eqx", "The 'initiator' field can be compared with the 'target' field only.");

                if (initiator.Nex != null && initiator.Nex != "target")
                    return new BadRequest($"{nameof(initiator)}.nex", "The 'initiator' field can be compared with the 'target' field only.");

                if (initiator.Eq == -1 || initiator.In?.Count == 0 && !initiator.InHasNull)
                    return Ok(Enumerable.Empty<TransactionOperation>());
            }

            if (sender != null)
            {
                if (sender.Eqx != null && sender.Eqx != "target")
                    return new BadRequest($"{nameof(sender)}.eqx", "The 'sender' field can be compared with the 'target' field only.");

                if (sender.Nex != null && sender.Nex != "target")
                    return new BadRequest($"{nameof(sender)}.nex", "The 'sender' field can be compared with the 'target' field only.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<TransactionOperation>());
            }

            if (target != null)
            {
                if (target.Eqx != null && target.Eqx != "sender" && target.Eqx != "initiator")
                    return new BadRequest($"{nameof(target)}.eqx", "The 'target' field can be compared with the 'sender' or 'initiator' fields only.");

                if (target.Nex != null && target.Nex != "sender" && target.Eqx != "initiator")
                    return new BadRequest($"{nameof(target)}.nex", "The 'target' field can be compared with the 'sender' or 'initiator' fields only.");

                if (target.Eq == -1 || target.In?.Count == 0 && !target.InHasNull)
                    return Ok(Enumerable.Empty<TransactionOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "storageUsed", "bakerFee", "storageFee", "allocationFee", "amount"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("anyof", anyof), ("initiator", initiator), ("sender", sender), ("target", target), 
                ("amount", amount), ("id", id),  ("level", level), ("timestamp", timestamp), ("codeHash", codeHash),
                ("senderCodeHash", senderCodeHash), ("targetCodeHash", targetCodeHash), ("entrypoint", entrypoint),
                ("parameter", parameter), ("hasInternals", hasInternals), ("status", status), ("select", select),
                ("sort", sort), ("offset", offset), ("limit", limit), ("micheline", micheline), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetTransactions(null, anyof, initiator, sender, target, amount, id, level, timestamp, codeHash, senderCodeHash, targetCodeHash, entrypoint, parameter, hasInternals, status, sort, offset, limit, micheline, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetTransactions(anyof, initiator, sender, target, amount, id, level, timestamp, codeHash, senderCodeHash, targetCodeHash, entrypoint, parameter, hasInternals, status, sort, offset, limit, select.Values[0], micheline, quote);
                else
                    res = await Operations.GetTransactions(anyof, initiator, sender, target, amount, id, level, timestamp, codeHash, senderCodeHash, targetCodeHash, entrypoint, parameter, hasInternals, status, sort, offset, limit, select.Values, micheline, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetTransactions(anyof, initiator, sender, target, amount, id, level, timestamp, codeHash, senderCodeHash, targetCodeHash, entrypoint, parameter, hasInternals, status, sort, offset, limit, select.Fields[0], micheline, quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetTransactions(anyof, initiator, sender, target, amount, id, level, timestamp, codeHash, senderCodeHash, targetCodeHash, entrypoint, parameter, hasInternals, status, sort, offset, limit, select.Fields, micheline, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get transaction by hash
        /// </summary>
        /// <remarks>
        /// Returns transaction operations with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("transactions/{hash}")]
        public async Task<ActionResult<IEnumerable<TransactionOperation>>> GetTransactionByHash(
            [Required][OpHash] string hash,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("micheline", micheline), ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetTransactions(hash, micheline, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get transaction by hash and counter
        /// </summary>
        /// <remarks>
        /// Returns transaction operations with specified hash and counter.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="counter">Operation counter</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("transactions/{hash}/{counter}")]
        public async Task<ActionResult<IEnumerable<TransactionOperation>>> GetTransactionByHashCounter(
            [Required][OpHash] string hash,
            [Min(0)] int counter,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("micheline", micheline), ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetTransactions(hash, counter, micheline, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get transaction by hash, counter and nonce
        /// </summary>
        /// <remarks>
        /// Returns an internal transaction operation with specified hash, counter and nonce.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="counter">Operation counter</param>
        /// <param name="nonce">Operation nonce (internal)</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("transactions/{hash}/{counter}/{nonce}")]
        public async Task<ActionResult<IEnumerable<TransactionOperation>>> GetTransactionByHashCounterNonce(
            [Required][OpHash] string hash,
            [Min(0)] int counter,
            [Min(0)] int nonce,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {          
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("micheline", micheline), ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetTransactions(hash, counter, nonce, micheline, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get transaction status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("transactions/{hash}/status")]
        public async Task<ActionResult<bool?>> GetTransactionStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetTransactionStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get transactions count
        /// </summary>
        /// <remarks>
        /// Returns the total number of transaction operations.
        /// </remarks>
        /// <param name="anyof">Filters transactions by any of the specified fields. Example: `anyof.sender.target=tz1...` will return operations where `sender` OR `target` is equal to the specified value. This parameter is useful when you need to retrieve all transactions associated with a specified account.</param>
        /// <param name="initiator">Filters transactions by initiator. Allowed fields for `.eqx` mode: `target`.</param>
        /// <param name="sender">Filters transactions by sender. Allowed fields for `.eqx` mode: `target`.</param>
        /// <param name="target">Filters transactions by target. Allowed fields for `.eqx` mode: `sender`, `initiator`.</param>
        /// <param name="amount">Filters transactions by amount (micro tez).</param>
        /// <param name="level">Filters transactions by level.</param>
        /// <param name="timestamp">Filters transactions by timestamp.</param>
        /// <param name="entrypoint">Filters transactions by entrypoint called on the target contract.</param>
        /// <param name="status">Filters transactions by operation status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="parameter">Filters transactions by parameter value. Note, this query parameter supports the following format: `?parameter{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?parameter.token_id=...` or `?parameter.sigs.0.ne=...`.
        /// Also, note that `.value` part must be omitted in the path, so, for example, filtering by `parameter.value.foo` must be specified as `?parameter.foo=...`.</param>
        /// <returns></returns>
        [HttpGet("transactions/count")]
        public async Task<ActionResult<int>> GetTransactionsCount(
            [OpenApiExtensionData("x-tzkt-extension", "anyof-parameter")]
            [OpenApiExtensionData("x-tzkt-anyof-parameter", "sender,target,initiator")]
            AnyOfParameter? anyof,
            AccountParameter? initiator,
            AccountParameter? sender,
            AccountParameter? target,
            Int64Parameter? amount,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            StringParameter? entrypoint,
            JsonParameter? parameter,
            OperationStatusParameter? status)
        {
            if (anyof == null &&
                initiator == null &&
                sender == null &&
                target == null &&
                amount == null &&
                level == null &&
                timestamp == null &&
                entrypoint == null &&
                parameter == null &&
                status == null)
                return Ok(State.Current.TransactionOpsCount);
        
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("anyof", anyof), ("initiator", initiator), ("sender", sender), ("target", target), ("amount", amount), 
                ("level", level), ("timestamp", timestamp), ("entrypoint", entrypoint), ("parameter", parameter), ("status", status));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetTransactionsCount(anyof, initiator, sender, target, amount, level, timestamp, entrypoint, parameter, status);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region reveals
        /// <summary>
        /// Get reveals
        /// </summary>
        /// <remarks>
        /// Returns a list of reveal operations.
        /// </remarks>
        /// <param name="sender">Filters reveal operations by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters reveal operations by level.</param>
        /// <param name="timestamp">Filters reveal operations by timestamp.</param>
        /// <param name="status">Filters reveal operations by operation status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts reveals by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `bakerFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("reveals")]
        public async Task<ActionResult<IEnumerable<RevealOperation>>> GetReveals(
            AccountParameter? sender,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.nex", "This parameter doesn't support .nex mode.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<RevealOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "bakerFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("sender", sender), ("level", level), ("timestamp", timestamp), ("status", status),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetReveals(null, sender, level, timestamp, status, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetReveals(sender, level, timestamp, status, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetReveals(sender, level, timestamp, status, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetReveals(sender, level, timestamp, status, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetReveals(sender, level, timestamp, status, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get reveal by hash
        /// </summary>
        /// <remarks>
        /// Returns reveal operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("reveals/{hash}")]
        public async Task<ActionResult<IEnumerable<RevealOperation>>> GetRevealByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetReveals(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get reveal status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("reveals/{hash}/status")]
        public async Task<ActionResult<bool?>> GetRevealStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetRevealStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get reveals count
        /// </summary>
        /// <remarks>
        /// Returns the total number of reveal operations.
        /// </remarks>
        /// <param name="level">Filters reveals by level.</param>
        /// <param name="timestamp">Filters reveals by timestamp.</param>
        /// <returns></returns>
        [HttpGet("reveals/count")]
        public async Task<ActionResult<int>> GetRevealsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.RevealOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetRevealsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region register constants
        /// <summary>
        /// Get register constants
        /// </summary>
        /// <remarks>
        /// Returns a list of register global constant operations.
        /// </remarks>
        /// <param name="sender">Filters operations by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="address">Filters operations by global address of the created constant (starts with `expr..`).</param>
        /// <param name="level">Filters operations by level.</param>
        /// <param name="timestamp">Filters operations by timestamp.</param>
        /// <param name="status">Filters operations by status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts operations by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `storageUsed`, `bakerFee`, `storageFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the constant value: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("register_constants")]
        public async Task<ActionResult<IEnumerable<RegisterConstantOperation>>> GetRegisterConstants(
            AccountParameter? sender,
            ExpressionParameter? address,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.nex", "This parameter doesn't support .nex mode.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<RegisterConstantOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "storageUsed", "bakerFee", "storageFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("sender", sender), ("address", address), ("level", level), ("timestamp", timestamp), ("status", status),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("micheline", micheline), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetRegisterConstants(null, sender, address, level, timestamp, status, sort, offset, limit, micheline, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetRegisterConstants(sender, address, level, timestamp, status, sort, offset, limit, select.Values[0], micheline, quote);
                else
                    res = await Operations.GetRegisterConstants(sender, address, level, timestamp, status, sort, offset, limit, select.Values, micheline, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetRegisterConstants(sender, address, level, timestamp, status, sort, offset, limit, select.Fields[0], micheline, quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetRegisterConstants(sender, address, level, timestamp, status, sort, offset, limit, select.Fields, micheline, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get register constant by hash
        /// </summary>
        /// <remarks>
        /// Returns register global constant operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="micheline">Format of the constant value: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("register_constants/{hash}")]
        public async Task<ActionResult<IEnumerable<RegisterConstantOperation>>> GetRegisterConstantByHash(
            [Required][OpHash] string hash,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("micheline", micheline), ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetRegisterConstants(hash, micheline, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get register constant status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("register_constants/{hash}/status")]
        public async Task<ActionResult<bool?>> GetRegisterConstantStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetRegisterConstantStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get register constants count
        /// </summary>
        /// <remarks>
        /// Returns the total number of register global constant operations.
        /// </remarks>
        /// <param name="level">Filters operations by level.</param>
        /// <param name="timestamp">Filters operations by timestamp.</param>
        /// <returns></returns>
        [HttpGet("register_constants/count")]
        public async Task<ActionResult<int>> GetRegisterConstantsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.RegisterConstantOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetRegisterConstantsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region set deposits limits
        /// <summary>
        /// Get set deposits limits
        /// </summary>
        /// <remarks>
        /// Returns a list of set deposits limit operations.
        /// </remarks>
        /// <param name="sender">Filters by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="status">Filters by status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `bakerFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("set_deposits_limits")]
        public async Task<ActionResult<IEnumerable<SetDepositsLimitOperation>>> GetSetDepositsLimits(
            AccountParameter? sender,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.nex", "This parameter doesn't support .nex mode.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<RegisterConstantOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "bakerFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("sender", sender), ("level", level), ("timestamp", timestamp), ("status", status),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetSetDepositsLimits(null, sender, level, timestamp, status, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetSetDepositsLimits(sender, level, timestamp, status, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetSetDepositsLimits(sender, level, timestamp, status, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetSetDepositsLimits(sender, level, timestamp, status, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetSetDepositsLimits(sender, level, timestamp, status, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get set deposits limit by hash
        /// </summary>
        /// <remarks>
        /// Returns set deposits limit operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("set_deposits_limits/{hash}")]
        public async Task<ActionResult<IEnumerable<SetDepositsLimitOperation>>> GetSetDepositsLimitByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetSetDepositsLimits(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get set deposits limit status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("set_deposits_limits/{hash}/status")]
        public async Task<ActionResult<bool?>> GetSetDepositsLimitStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetSetDepositsLimitStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get set deposits limits count
        /// </summary>
        /// <remarks>
        /// Returns the total number of set deposits limit operations.
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("set_deposits_limits/count")]
        public async Task<ActionResult<int>> GetSetDepositsLimitsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.SetDepositsLimitOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetSetDepositsLimitsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region transfer ticket
        /// <summary>
        /// Get transfer ticket
        /// </summary>
        /// <remarks>
        /// Returns a list of transfer ticket operations.
        /// </remarks>
        /// <param name="anyof">Filters by any of the specified fields. Example: `anyof.sender.target=tz1...` will return operations where `sender` OR `target` is equal to the specified value. This parameter is useful when you need to retrieve all operations associated with a specified account.</param>
        /// <param name="sender">Filters by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="target">Filters by target. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="id">Filters by id.</param>
        /// <param name="ticketer">Filters by ticketer. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="status">Filters by status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `bakerFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the content value: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("transfer_ticket")]
        public async Task<ActionResult<IEnumerable<TransferTicketOperation>>> GetTransferTicketOps(
            [OpenApiExtensionData("x-tzkt-extension", "anyof-parameter")]
            [OpenApiExtensionData("x-tzkt-anyof-parameter", "sender,target,ticketer")]
            AnyOfParameter? anyof,
            AccountParameter? sender,
            AccountParameter? target,
            Int64Parameter? id,
            AccountParameter? ticketer,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.nex", "This parameter doesn't support .nex mode.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<TransferTicketOperation>());
            }

            if (target != null)
            {
                if (target.Eqx != null)
                    return new BadRequest($"{nameof(target)}.eqx", "This parameter doesn't support .eqx mode.");

                if (target.Nex != null)
                    return new BadRequest($"{nameof(target)}.nex", "This parameter doesn't support .nex mode.");

                if (target.Eq == -1 || target.In?.Count == 0 || target.Null == true)
                    return Ok(Enumerable.Empty<TransferTicketOperation>());
            }

            if (ticketer != null)
            {
                if (ticketer.Eqx != null)
                    return new BadRequest($"{nameof(ticketer)}.eqx", "This parameter doesn't support .eqx mode.");

                if (ticketer.Nex != null)
                    return new BadRequest($"{nameof(ticketer)}.nex", "This parameter doesn't support .nex mode.");

                if (ticketer.Eq == -1 || ticketer.In?.Count == 0 || ticketer.Null == true)
                    return Ok(Enumerable.Empty<TransferTicketOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "bakerFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetTransferTicketOps(null, anyof, sender, target, ticketer, id, level, timestamp, status, sort, offset, limit, micheline, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetTransferTicketOps(anyof, sender, target, ticketer, id, level, timestamp, status, sort, offset, limit, select.Values[0], micheline, quote));
                else
                    return Ok(await Operations.GetTransferTicketOps(anyof, sender, target, ticketer, id, level, timestamp, status, sort, offset, limit, select.Values, micheline, quote));
            }
            else
            {
                if (select.Fields!.Length == 1)
                    return Ok(await Operations.GetTransferTicketOps(anyof, sender, target, ticketer, id, level, timestamp, status, sort, offset, limit, select.Fields[0], micheline, quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetTransferTicketOps(anyof, sender, target, ticketer, id, level, timestamp, status, sort, offset, limit, select.Fields, micheline, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get transfer ticket by hash
        /// </summary>
        /// <remarks>
        /// Returns transfer ticket operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="micheline">Format of the content value: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("transfer_ticket/{hash}")]
        public Task<IEnumerable<TransferTicketOperation>> GetTransferTicketOpsByHash(
            [Required][OpHash] string hash,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            return Operations.GetTransferTicketOps(hash, micheline, quote);
        }

        /// <summary>
        /// Get transfer ticket status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("transfer_ticket/{hash}/status")]
        public async Task<ActionResult<bool?>> GetTransferTicketStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetTransferTicketStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get transfer ticket count
        /// </summary>
        /// <remarks>
        /// Returns the total number of transfer ticket operations.
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("transfer_ticket/count")]
        public Task<int> GetTransferTicketOpsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.TransferTicketOpsCount);

            return Operations.GetTransferTicketOpsCount(level, timestamp);
        }
        #endregion

        #region tx rollup commit
        /// <summary>
        /// Get tx rollup commit
        /// </summary>
        /// <remarks>
        /// Returns a list of tx rollup commit operations.
        /// </remarks>
        /// <param name="sender">Filters by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="rollup">Filters by rollup. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="status">Filters by status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `bakerFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_commit")]
        public async Task<ActionResult<IEnumerable<TxRollupCommitOperation>>> GetTxRollupCommitOps(
            AccountParameter? sender,
            AccountParameter? rollup,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.nex", "This parameter doesn't support .nex mode.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<TxRollupCommitOperation>());
            }

            if (rollup != null)
            {
                if (rollup.Eqx != null)
                    return new BadRequest($"{nameof(rollup)}.eqx", "This parameter doesn't support .eqx mode.");

                if (rollup.Nex != null)
                    return new BadRequest($"{nameof(rollup)}.nex", "This parameter doesn't support .nex mode.");

                if (rollup.Eq == -1 || rollup.In?.Count == 0 || rollup.Null == true)
                    return Ok(Enumerable.Empty<TxRollupCommitOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "bakerFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetTxRollupCommitOps(sender, rollup, level, timestamp, status, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetTxRollupCommitOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetTxRollupCommitOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields!.Length == 1)
                    return Ok(await Operations.GetTxRollupCommitOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetTxRollupCommitOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get tx rollup commit by hash
        /// </summary>
        /// <remarks>
        /// Returns tx rollup commit operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_commit/{hash}")]
        public Task<IEnumerable<TxRollupCommitOperation>> GetTxRollupCommitOpsByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            return Operations.GetTxRollupCommitOps(hash, quote);
        }

        /// <summary>
        /// Get tx rollup commit status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_commit/{hash}/status")]
        public async Task<ActionResult<bool?>> GetTxRollupCommitStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetTxRollupCommitStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get tx rollup commit count
        /// </summary>
        /// <remarks>
        /// Returns the total number of tx rollup commit operations.
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_commit/count")]
        public Task<int> GetTxRollupCommitOpsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.TxRollupCommitOpsCount);

            return Operations.GetTxRollupCommitOpsCount(level, timestamp);
        }
        #endregion

        #region tx rollup dispatch tickets
        /// <summary>
        /// Get tx rollup dispatch tickets
        /// </summary>
        /// <remarks>
        /// Returns a list of tx rollup dispatch tickets operations.
        /// </remarks>
        /// <param name="sender">Filters by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="rollup">Filters by rollup. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="status">Filters by status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `bakerFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_dispatch_tickets")]
        public async Task<ActionResult<IEnumerable<TxRollupDispatchTicketsOperation>>> GetTxRollupDispatchTicketsOps(
            AccountParameter? sender,
            AccountParameter? rollup,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.nex", "This parameter doesn't support .nex mode.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<TxRollupDispatchTicketsOperation>());
            }

            if (rollup != null)
            {
                if (rollup.Eqx != null)
                    return new BadRequest($"{nameof(rollup)}.eqx", "This parameter doesn't support .eqx mode.");

                if (rollup.Nex != null)
                    return new BadRequest($"{nameof(rollup)}.nex", "This parameter doesn't support .nex mode.");

                if (rollup.Eq == -1 || rollup.In?.Count == 0 || rollup.Null == true)
                    return Ok(Enumerable.Empty<TxRollupDispatchTicketsOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "bakerFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetTxRollupDispatchTicketsOps(sender, rollup, level, timestamp, status, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetTxRollupDispatchTicketsOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetTxRollupDispatchTicketsOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields!.Length == 1)
                    return Ok(await Operations.GetTxRollupDispatchTicketsOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetTxRollupDispatchTicketsOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get tx rollup dispatch tickets by hash
        /// </summary>
        /// <remarks>
        /// Returns tx rollup dispatch tickets operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_dispatch_tickets/{hash}")]
        public Task<IEnumerable<TxRollupDispatchTicketsOperation>> GetTxRollupDispatchTicketsOpsByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            return Operations.GetTxRollupDispatchTicketsOps(hash, quote);
        }

        /// <summary>
        /// Get tx rollup dispatch tickets status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_dispatch_tickets/{hash}/status")]
        public async Task<ActionResult<bool?>> GetTxRollupDispatchTicketsStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetTxRollupDispatchTicketsStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get tx rollup dispatch tickets count
        /// </summary>
        /// <remarks>
        /// Returns the total number of tx rollup dispatch tickets operations.
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_dispatch_tickets/count")]
        public Task<int> GetTxRollupDispatchTicketsOpsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.TxRollupDispatchTicketsOpsCount);

            return Operations.GetTxRollupDispatchTicketsOpsCount(level, timestamp);
        }
        #endregion

        #region tx rollup finalize commitment
        /// <summary>
        /// Get tx rollup finalize commitment
        /// </summary>
        /// <remarks>
        /// Returns a list of tx rollup finalize commitment operations.
        /// </remarks>
        /// <param name="sender">Filters by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="rollup">Filters by rollup. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="status">Filters by status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `bakerFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_finalize_commitment")]
        public async Task<ActionResult<IEnumerable<TxRollupFinalizeCommitmentOperation>>> GetTxRollupFinalizeCommitmentOps(
            AccountParameter? sender,
            AccountParameter? rollup,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.nex", "This parameter doesn't support .nex mode.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<TxRollupFinalizeCommitmentOperation>());
            }

            if (rollup != null)
            {
                if (rollup.Eqx != null)
                    return new BadRequest($"{nameof(rollup)}.eqx", "This parameter doesn't support .eqx mode.");

                if (rollup.Nex != null)
                    return new BadRequest($"{nameof(rollup)}.nex", "This parameter doesn't support .nex mode.");

                if (rollup.Eq == -1 || rollup.In?.Count == 0 || rollup.Null == true)
                    return Ok(Enumerable.Empty<TxRollupFinalizeCommitmentOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "bakerFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetTxRollupFinalizeCommitmentOps(sender, rollup, level, timestamp, status, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetTxRollupFinalizeCommitmentOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetTxRollupFinalizeCommitmentOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields!.Length == 1)
                    return Ok(await Operations.GetTxRollupFinalizeCommitmentOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetTxRollupFinalizeCommitmentOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get tx rollup finalize commitment by hash
        /// </summary>
        /// <remarks>
        /// Returns tx rollup finalize commitment operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_finalize_commitment/{hash}")]
        public Task<IEnumerable<TxRollupFinalizeCommitmentOperation>> GetTxRollupFinalizeCommitmentOpsByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            return Operations.GetTxRollupFinalizeCommitmentOps(hash, quote);
        }

        /// <summary>
        /// Get tx rollup finalize commitment status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_finalize_commitment/{hash}/status")]
        public async Task<ActionResult<bool?>> GetTxRollupFinalizeCommitmentStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetTxRollupFinalizeCommitmentStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get tx rollup finalize commitment count
        /// </summary>
        /// <remarks>
        /// Returns the total number of tx rollup finalize commitment operations.
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_finalize_commitment/count")]
        public Task<int> GetTxRollupFinalizeCommitmentOpsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.TxRollupFinalizeCommitmentOpsCount);

            return Operations.GetTxRollupFinalizeCommitmentOpsCount(level, timestamp);
        }
        #endregion

        #region tx rollup origination
        /// <summary>
        /// Get tx rollup origination
        /// </summary>
        /// <remarks>
        /// Returns a list of tx rollup origination operations.
        /// </remarks>
        /// <param name="sender">Filters by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="rollup">Filters by rollup. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="status">Filters by status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `bakerFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_origination")]
        public async Task<ActionResult<IEnumerable<TxRollupOriginationOperation>>> GetTxRollupOriginationOps(
            AccountParameter? sender,
            AccountParameter? rollup,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.nex", "This parameter doesn't support .nex mode.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<TxRollupOriginationOperation>());
            }

            if (rollup != null)
            {
                if (rollup.Eqx != null)
                    return new BadRequest($"{nameof(rollup)}.eqx", "This parameter doesn't support .eqx mode.");

                if (rollup.Nex != null)
                    return new BadRequest($"{nameof(rollup)}.nex", "This parameter doesn't support .nex mode.");

                if (rollup.Eq == -1 || rollup.In?.Count == 0 || rollup.Null == true)
                    return Ok(Enumerable.Empty<TxRollupOriginationOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "bakerFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetTxRollupOriginationOps(sender, rollup, level, timestamp, status, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetTxRollupOriginationOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetTxRollupOriginationOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields!.Length == 1)
                    return Ok(await Operations.GetTxRollupOriginationOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetTxRollupOriginationOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get tx rollup origination by hash
        /// </summary>
        /// <remarks>
        /// Returns tx rollup origination operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_origination/{hash}")]
        public Task<IEnumerable<TxRollupOriginationOperation>> GetTxRollupOriginationOpsByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            return Operations.GetTxRollupOriginationOps(hash, quote);
        }

        /// <summary>
        /// Get tx rollup origination status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_origination/{hash}/status")]
        public async Task<ActionResult<bool?>> GetTxRollupOriginationStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetTxRollupOriginationStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get tx rollup origination count
        /// </summary>
        /// <remarks>
        /// Returns the total number of tx rollup origination operations.
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_origination/count")]
        public Task<int> GetTxRollupOriginationOpsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.TxRollupOriginationOpsCount);

            return Operations.GetTxRollupOriginationOpsCount(level, timestamp);
        }
        #endregion

        #region tx rollup rejection
        /// <summary>
        /// Get tx rollup rejection
        /// </summary>
        /// <remarks>
        /// Returns a list of tx rollup rejection operations.
        /// </remarks>
        /// <param name="anyof">Filters by any of the specified fields. Example: `anyof.sender.committer=tz1...` will return operations where `sender` OR `committer` is equal to the specified value. This parameter is useful when you need to retrieve all operations associated with a specified account.</param>
        /// <param name="sender">Filters by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="committer">Filters by committer. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="rollup">Filters by rollup. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="status">Filters by status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `bakerFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_rejection")]
        public async Task<ActionResult<IEnumerable<TxRollupRejectionOperation>>> GetTxRollupRejectionOps(
            [OpenApiExtensionData("x-tzkt-extension", "anyof-parameter")]
            [OpenApiExtensionData("x-tzkt-anyof-parameter", "sender,committer")]
            AnyOfParameter? anyof,
            AccountParameter? sender,
            AccountParameter? committer,
            AccountParameter? rollup,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.nex", "This parameter doesn't support .nex mode.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<TxRollupRejectionOperation>());
            }

            if (committer != null)
            {
                if (committer.Eqx != null)
                    return new BadRequest($"{nameof(committer)}.eqx", "This parameter doesn't support .eqx mode.");

                if (committer.Nex != null)
                    return new BadRequest($"{nameof(committer)}.nex", "This parameter doesn't support .nex mode.");

                if (committer.Eq == -1 || committer.In?.Count == 0 || committer.Null == true)
                    return Ok(Enumerable.Empty<TxRollupRejectionOperation>());
            }

            if (rollup != null)
            {
                if (rollup.Eqx != null)
                    return new BadRequest($"{nameof(rollup)}.eqx", "This parameter doesn't support .eqx mode.");

                if (rollup.Nex != null)
                    return new BadRequest($"{nameof(rollup)}.nex", "This parameter doesn't support .nex mode.");

                if (rollup.Eq == -1 || rollup.In?.Count == 0 || rollup.Null == true)
                    return Ok(Enumerable.Empty<TxRollupRejectionOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "bakerFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetTxRollupRejectionOps(anyof, sender, committer, rollup, level, timestamp, status, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetTxRollupRejectionOps(anyof, sender, committer, rollup, level, timestamp, status, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetTxRollupRejectionOps(anyof, sender, committer, rollup, level, timestamp, status, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields!.Length == 1)
                    return Ok(await Operations.GetTxRollupRejectionOps(anyof, sender, committer, rollup, level, timestamp, status, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetTxRollupRejectionOps(anyof, sender, committer, rollup, level, timestamp, status, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get tx rollup rejection by hash
        /// </summary>
        /// <remarks>
        /// Returns tx rollup rejection operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_rejection/{hash}")]
        public Task<IEnumerable<TxRollupRejectionOperation>> GetTxRollupRejectionOpsByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            return Operations.GetTxRollupRejectionOps(hash, quote);
        }

        /// <summary>
        /// Get tx rollup rejection status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_rejection/{hash}/status")]
        public async Task<ActionResult<bool?>> GetTxRollupRejectionStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetTxRollupRejectionStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get tx rollup rejection count
        /// </summary>
        /// <remarks>
        /// Returns the total number of tx rollup rejection operations.
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_rejection/count")]
        public Task<int> GetTxRollupRejectionOpsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.TxRollupRejectionOpsCount);

            return Operations.GetTxRollupRejectionOpsCount(level, timestamp);
        }
        #endregion

        #region tx rollup remove commitment
        /// <summary>
        /// Get tx rollup remove commitment
        /// </summary>
        /// <remarks>
        /// Returns a list of tx rollup remove commitment operations.
        /// </remarks>
        /// <param name="sender">Filters by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="rollup">Filters by rollup. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="status">Filters by status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `bakerFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_remove_commitment")]
        public async Task<ActionResult<IEnumerable<TxRollupRemoveCommitmentOperation>>> GetTxRollupRemoveCommitmentOps(
            AccountParameter? sender,
            AccountParameter? rollup,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.nex", "This parameter doesn't support .nex mode.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<TxRollupRemoveCommitmentOperation>());
            }

            if (rollup != null)
            {
                if (rollup.Eqx != null)
                    return new BadRequest($"{nameof(rollup)}.eqx", "This parameter doesn't support .eqx mode.");

                if (rollup.Nex != null)
                    return new BadRequest($"{nameof(rollup)}.nex", "This parameter doesn't support .nex mode.");

                if (rollup.Eq == -1 || rollup.In?.Count == 0 || rollup.Null == true)
                    return Ok(Enumerable.Empty<TxRollupRemoveCommitmentOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "bakerFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetTxRollupRemoveCommitmentOps(sender, rollup, level, timestamp, status, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetTxRollupRemoveCommitmentOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetTxRollupRemoveCommitmentOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields!.Length == 1)
                    return Ok(await Operations.GetTxRollupRemoveCommitmentOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetTxRollupRemoveCommitmentOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get tx rollup remove commitment by hash
        /// </summary>
        /// <remarks>
        /// Returns tx rollup remove commitment operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_remove_commitment/{hash}")]
        public Task<IEnumerable<TxRollupRemoveCommitmentOperation>> GetTxRollupRemoveCommitmentOpsByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            return Operations.GetTxRollupRemoveCommitmentOps(hash, quote);
        }

        /// <summary>
        /// Get tx rollup remove commitment status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_remove_commitment/{hash}/status")]
        public async Task<ActionResult<bool?>> GetTxRollupRemoveCommitmentStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetTxRollupRemoveCommitmentStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get tx rollup remove commitment count
        /// </summary>
        /// <remarks>
        /// Returns the total number of tx rollup remove commitment operations.
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_remove_commitment/count")]
        public Task<int> GetTxRollupRemoveCommitmentOpsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.TxRollupRemoveCommitmentOpsCount);

            return Operations.GetTxRollupRemoveCommitmentOpsCount(level, timestamp);
        }
        #endregion

        #region tx rollup return bond
        /// <summary>
        /// Get tx rollup return bond
        /// </summary>
        /// <remarks>
        /// Returns a list of tx rollup return bond operations.
        /// </remarks>
        /// <param name="sender">Filters by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="rollup">Filters by rollup. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="status">Filters by status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `bakerFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_return_bond")]
        public async Task<ActionResult<IEnumerable<TxRollupReturnBondOperation>>> GetTxRollupReturnBondOps(
            AccountParameter? sender,
            AccountParameter? rollup,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.nex", "This parameter doesn't support .nex mode.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<TxRollupReturnBondOperation>());
            }

            if (rollup != null)
            {
                if (rollup.Eqx != null)
                    return new BadRequest($"{nameof(rollup)}.eqx", "This parameter doesn't support .eqx mode.");

                if (rollup.Nex != null)
                    return new BadRequest($"{nameof(rollup)}.nex", "This parameter doesn't support .nex mode.");

                if (rollup.Eq == -1 || rollup.In?.Count == 0 || rollup.Null == true)
                    return Ok(Enumerable.Empty<TxRollupReturnBondOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "bakerFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetTxRollupReturnBondOps(sender, rollup, level, timestamp, status, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetTxRollupReturnBondOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetTxRollupReturnBondOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields!.Length == 1)
                    return Ok(await Operations.GetTxRollupReturnBondOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetTxRollupReturnBondOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get tx rollup return bond by hash
        /// </summary>
        /// <remarks>
        /// Returns tx rollup return bond operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_return_bond/{hash}")]
        public Task<IEnumerable<TxRollupReturnBondOperation>> GetTxRollupReturnBondOpsByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            return Operations.GetTxRollupReturnBondOps(hash, quote);
        }

        /// <summary>
        /// Get tx rollup return bond status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_return_bond/{hash}/status")]
        public async Task<ActionResult<bool?>> GetTxRollupReturnBondStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetTxRollupReturnBondStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get tx rollup return bond count
        /// </summary>
        /// <remarks>
        /// Returns the total number of tx rollup return bond operations.
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_return_bond/count")]
        public Task<int> GetTxRollupReturnBondOpsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.TxRollupReturnBondOpsCount);

            return Operations.GetTxRollupReturnBondOpsCount(level, timestamp);
        }
        #endregion

        #region tx rollup submit batch
        /// <summary>
        /// Get tx rollup submit batch
        /// </summary>
        /// <remarks>
        /// Returns a list of tx rollup submit batch operations.
        /// </remarks>
        /// <param name="sender">Filters by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="rollup">Filters by rollup. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="status">Filters by status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `bakerFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_submit_batch")]
        public async Task<ActionResult<IEnumerable<TxRollupSubmitBatchOperation>>> GetTxRollupSubmitBatchOps(
            AccountParameter? sender,
            AccountParameter? rollup,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.nex", "This parameter doesn't support .nex mode.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<TxRollupSubmitBatchOperation>());
            }

            if (rollup != null)
            {
                if (rollup.Eqx != null)
                    return new BadRequest($"{nameof(rollup)}.eqx", "This parameter doesn't support .eqx mode.");

                if (rollup.Nex != null)
                    return new BadRequest($"{nameof(rollup)}.nex", "This parameter doesn't support .nex mode.");

                if (rollup.Eq == -1 || rollup.In?.Count == 0 || rollup.Null == true)
                    return Ok(Enumerable.Empty<TxRollupSubmitBatchOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "bakerFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetTxRollupSubmitBatchOps(sender, rollup, level, timestamp, status, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetTxRollupSubmitBatchOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetTxRollupSubmitBatchOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields!.Length == 1)
                    return Ok(await Operations.GetTxRollupSubmitBatchOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetTxRollupSubmitBatchOps(sender, rollup, level, timestamp, status, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get tx rollup submit batch by hash
        /// </summary>
        /// <remarks>
        /// Returns tx rollup submit batch operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_submit_batch/{hash}")]
        public Task<IEnumerable<TxRollupSubmitBatchOperation>> GetTxRollupSubmitBatchOpsByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            return Operations.GetTxRollupSubmitBatchOps(hash, quote);
        }

        /// <summary>
        /// Get tx rollup submit batch status
        /// </summary>
        /// <remarks>
        /// Returns operation status: `true` if applied, `false` if failed, `null` (or HTTP 204) if doesn't exist.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_submit_batch/{hash}/status")]
        public async Task<ActionResult<bool?>> GetTxRollupSubmitBatchStatus([Required][OpHash] string hash)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetTxRollupSubmitBatchStatus(hash);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get tx rollup submit batch count
        /// </summary>
        /// <remarks>
        /// Returns the total number of tx rollup submit batch operations.
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("tx_rollup_submit_batch/count")]
        public Task<int> GetTxRollupSubmitBatchOpsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.TxRollupSubmitBatchOpsCount);

            return Operations.GetTxRollupSubmitBatchOpsCount(level, timestamp);
        }
        #endregion

        #region increase paid storage
        /// <summary>
        /// Get increase paid storage
        /// </summary>
        /// <remarks>
        /// Returns a list of increase paid storage operations.
        /// </remarks>
        /// <param name="sender">Filters by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="contract">Filters by contract. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="status">Filters by status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts operations by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `storageUsed`, `bakerFee`, `storageFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("increase_paid_storage")]
        public async Task<ActionResult<IEnumerable<IncreasePaidStorageOperation>>> GetIncreasePaidStorageOps(
            AccountParameter? sender,
            AccountParameter? contract,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.nex", "This parameter doesn't support .nex mode.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<IncreasePaidStorageOperation>());
            }

            if (contract != null)
            {
                if (contract.Eqx != null)
                    return new BadRequest($"{nameof(contract)}.eqx", "This parameter doesn't support .eqx mode.");

                if (contract.Nex != null)
                    return new BadRequest($"{nameof(contract)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "storageUsed", "bakerFee", "storageFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("sender", sender), ("contract", contract), ("level", level), ("timestamp", timestamp), ("status", status),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetIncreasePaidStorageOps(null, sender, contract, level, timestamp, status, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetIncreasePaidStorageOps(sender, contract, level, timestamp, status, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetIncreasePaidStorageOps(sender, contract, level, timestamp, status, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetIncreasePaidStorageOps(sender, contract, level, timestamp, status, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetIncreasePaidStorageOps(sender, contract, level, timestamp, status, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get increase paid storage by hash
        /// </summary>
        /// <remarks>
        /// Returns increase paid storage operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("increase_paid_storage/{hash}")]
        public async Task<ActionResult<IEnumerable<IncreasePaidStorageOperation>>> GetIncreasePaidStorageByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetIncreasePaidStorageOps(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get increase paid storage count
        /// </summary>
        /// <remarks>
        /// Returns the total number of increase paid storage operations.
        /// </remarks>
        /// <param name="level">Filters operations by level.</param>
        /// <param name="timestamp">Filters operations by timestamp.</param>
        /// <returns></returns>
        [HttpGet("increase_paid_storage/count")]
        public async Task<ActionResult<int>> GetIncreasePaidStorageCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.IncreasePaidStorageOpsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetIncreasePaidStorageOpsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region update consensus key
        /// <summary>
        /// Get update consensus key
        /// </summary>
        /// <remarks>
        /// Returns a list of update consensus key operations.
        /// </remarks>
        /// <param name="sender">Filters by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="activationCycle">Filters by activation cycle. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="publicKeyHash">Filters by pkh (tz address).</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="status">Filters by status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts operations by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `storageUsed`, `bakerFee`, `storageFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("update_consensus_key")]
        public async Task<ActionResult<IEnumerable<UpdateConsensusKeyOperation>>> GetUpdateConsensusKeyOps(
            AccountParameter? sender,
            Int32Parameter? activationCycle,
            AddressParameter? publicKeyHash,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.nex", "This parameter doesn't support .nex mode.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<UpdateConsensusKeyOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "storageUsed", "bakerFee", "storageFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("sender", sender), ("activationCycle", activationCycle), ("publicKeyHash", publicKeyHash),
                ("level", level), ("timestamp", timestamp), ("status", status),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetUpdateConsensusKeys(null, sender, activationCycle, publicKeyHash, level, timestamp, status, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetUpdateConsensusKeys(sender, activationCycle, publicKeyHash, level, timestamp, status, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetUpdateConsensusKeys(sender, activationCycle, publicKeyHash, level, timestamp, status, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetUpdateConsensusKeys(sender, activationCycle, publicKeyHash, level, timestamp, status, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetUpdateConsensusKeys(sender, activationCycle, publicKeyHash, level, timestamp, status, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get update consensus key by hash
        /// </summary>
        /// <remarks>
        /// Returns update consensus key operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("update_consensus_key/{hash}")]
        public async Task<ActionResult<IEnumerable<UpdateConsensusKeyOperation>>> GetUpdateConsensusKeyByHash(
            [Required][OpHash] string hash,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetUpdateConsensusKeys(hash, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get update consensus key count
        /// </summary>
        /// <remarks>
        /// Returns the total number of update consensus key operations.
        /// </remarks>
        /// <param name="level">Filters operations by level.</param>
        /// <param name="timestamp">Filters operations by timestamp.</param>
        /// <returns></returns>
        [HttpGet("update_consensus_key/count")]
        public async Task<ActionResult<int>> GetUpdateConsensusKeyOpsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.UpdateConsensusKeyOpsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetUpdateConsensusKeysCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region smart rollup add messages
        /// <summary>
        /// Get smart rollup add messages
        /// </summary>
        /// <remarks>
        /// Returns a list of smart rollup add messages operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("sr_add_messages")]
        public async Task<ActionResult<IEnumerable<SmartRollupAddMessagesOperation>>> GetSmartRollupAddMessagesOps(
            [FromQuery] ManagerOperationFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection,
            [FromQuery] Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection), ("quote", quote));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, selection.select == null
                    ? await Operations.GetSmartRollupAddMessagesOps(filter, pagination, quote)
                    : new SelectionResponse
                    {
                        Cols = selection.Cols,
                        Rows = await Operations.GetSmartRollupAddMessagesOps(filter, pagination, selection, quote)
                    });

            return this.Bytes(res);
        }

        /// <summary>
        /// Get smart rollup add messages count
        /// </summary>
        /// <remarks>
        /// Returns a total number of smart rollup add messages operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("sr_add_messages/count")]
        public async Task<ActionResult<int>> GetSmartRollupAddMessagesOpsCount([FromQuery] ManagerOperationFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.SmartRollupAddMessagesOpsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, await Operations.GetSmartRollupAddMessagesOpsCount(filter));

            return this.Bytes(res);
        }
        #endregion

        #region smart rollup cement
        /// <summary>
        /// Get smart rollup cement
        /// </summary>
        /// <remarks>
        /// Returns a list of smart rollup cement operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("sr_cement")]
        public async Task<ActionResult<IEnumerable<SmartRollupCementOperation>>> GetSmartRollupCementOps(
            [FromQuery] SrOperationFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection,
            [FromQuery] Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection), ("quote", quote));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, selection.select == null
                    ? await Operations.GetSmartRollupCementOps(filter, pagination, quote)
                    : new SelectionResponse
                    {
                        Cols = selection.Cols,
                        Rows = await Operations.GetSmartRollupCementOps(filter, pagination, selection, quote)
                    });

            return this.Bytes(res);
        }

        /// <summary>
        /// Get smart rollup cement count
        /// </summary>
        /// <remarks>
        /// Returns a total number of smart rollup cement operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("sr_cement/count")]
        public async Task<ActionResult<int>> GetSmartRollupCementOpsCount([FromQuery] SrOperationFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.SmartRollupCementOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, await Operations.GetSmartRollupCementOpsCount(filter));
            
            return this.Bytes(res);
        }
        #endregion

        #region smart rollup execute
        /// <summary>
        /// Get smart rollup execute
        /// </summary>
        /// <remarks>
        /// Returns a list of smart rollup execute operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("sr_execute")]
        public async Task<ActionResult<IEnumerable<SmartRollupExecuteOperation>>> GetSmartRollupExecuteOps(
            [FromQuery] SrOperationFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection,
            [FromQuery] Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection), ("quote", quote));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, selection.select == null
                    ? await Operations.GetSmartRollupExecuteOps(filter, pagination, quote)
                    : new SelectionResponse
                    {
                        Cols = selection.Cols,
                        Rows = await Operations.GetSmartRollupExecuteOps(filter, pagination, selection, quote)
                    });

            return this.Bytes(res);
        }

        /// <summary>
        /// Get smart rollup execute count
        /// </summary>
        /// <remarks>
        /// Returns a total number of smart rollup execute operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("sr_execute/count")]
        public async Task<ActionResult<int>> GetSmartRollupExecuteOpsCount([FromQuery] SrOperationFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.SmartRollupExecuteOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, await Operations.GetSmartRollupExecuteOpsCount(filter));

            return this.Bytes(res);
        }
        #endregion

        #region smart rollup originate
        /// <summary>
        /// Get smart rollup originate
        /// </summary>
        /// <remarks>
        /// Returns a list of smart rollup originate operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <param name="micheline">Format of the parameter type value: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <returns></returns>
        [HttpGet("sr_originate")]
        public async Task<ActionResult<IEnumerable<SmartRollupOriginateOperation>>> GetSmartRollupOriginateOps(
            [FromQuery] SrOperationFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection,
            [FromQuery] Symbols quote = Symbols.None,
            [FromQuery] MichelineFormat micheline = MichelineFormat.Json)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection), ("quote", quote), ("micheline", micheline));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, selection.select == null
                    ? await Operations.GetSmartRollupOriginateOps(filter, pagination, quote, micheline)
                    : new SelectionResponse
                    {
                        Cols = selection.Cols,
                        Rows = await Operations.GetSmartRollupOriginateOps(filter, pagination, selection, quote, micheline)
                    });
            
            return this.Bytes(res);
        }

        /// <summary>
        /// Get smart rollup originate count
        /// </summary>
        /// <remarks>
        /// Returns a total number of smart rollup originate operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("sr_originate/count")]
        public async Task<ActionResult<int>> GetSmartRollupOriginateOpsCount([FromQuery] SrOperationFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.SmartRollupOriginateOpsCount);
             
            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, await Operations.GetSmartRollupOriginateOpsCount(filter));

            return this.Bytes(res);
        }
        #endregion

        #region smart rollup publish
        /// <summary>
        /// Get smart rollup publish
        /// </summary>
        /// <remarks>
        /// Returns a list of smart rollup publish operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("sr_publish")]
        public async Task<ActionResult<IEnumerable<SmartRollupPublishOperation>>> GetSmartRollupPublishOps(
            [FromQuery] SrPublishOperationFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection,
            [FromQuery] Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection), ("quote", quote));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, selection.select == null
                    ? await Operations.GetSmartRollupPublishOps(filter, pagination, quote)
                    : new SelectionResponse
                    {
                        Cols = selection.Cols,
                        Rows = await Operations.GetSmartRollupPublishOps(filter, pagination, selection, quote)
                    });

            return this.Bytes(res);
        }

        /// <summary>
        /// Get smart rollup publish count
        /// </summary>
        /// <remarks>
        /// Returns a total number of smart rollup publish operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("sr_publish/count")]
        public async Task<ActionResult<int>> GetSmartRollupPublishOpsCount([FromQuery] SrPublishOperationFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.SmartRollupPublishOpsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, await Operations.GetSmartRollupPublishOpsCount(filter));

            return this.Bytes(res);
        }
        #endregion

        #region smart rollup recover bond
        /// <summary>
        /// Get smart rollup recover bond
        /// </summary>
        /// <remarks>
        /// Returns a list of smart rollup recover bond operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("sr_recover_bond")]
        public async Task<ActionResult<IEnumerable<SmartRollupRecoverBondOperation>>> GetSmartRollupRecoverBondOps(
            [FromQuery] SrRecoverBondOperationFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection,
            [FromQuery] Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection), ("quote", quote));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, selection.select == null
                    ? await Operations.GetSmartRollupRecoverBondOps(filter, pagination, quote)
                    : new SelectionResponse
                    {
                        Cols = selection.Cols,
                        Rows = await Operations.GetSmartRollupRecoverBondOps(filter, pagination, selection, quote)
                    });

            return this.Bytes(res);
        }

        /// <summary>
        /// Get smart rollup recover bond count
        /// </summary>
        /// <remarks>
        /// Returns a total number of smart rollup recover bond operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("sr_recover_bond/count")]
        public async Task<ActionResult<int>> GetSmartRollupRecoverBondOpsCount([FromQuery] SrRecoverBondOperationFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.SmartRollupRecoverBondOpsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, await Operations.GetSmartRollupRecoverBondOpsCount(filter));

            return this.Bytes(res);
        }
        #endregion

        #region smart rollup refute
        /// <summary>
        /// Get smart rollup refute
        /// </summary>
        /// <remarks>
        /// Returns a list of smart rollup refute operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("sr_refute")]
        public async Task<ActionResult<IEnumerable<SmartRollupRefuteOperation>>> GetSmartRollupRefuteOps(
            [FromQuery] SrRefuteOperationFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection,
            [FromQuery] Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection), ("quote", quote));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, selection.select == null
                    ? await Operations.GetSmartRollupRefuteOps(filter, pagination, quote)
                    : new SelectionResponse
                    {
                        Cols = selection.Cols,
                        Rows = await Operations.GetSmartRollupRefuteOps(filter, pagination, selection, quote)
                    });

            return this.Bytes(res);
        }

        /// <summary>
        /// Get smart rollup refute count
        /// </summary>
        /// <remarks>
        /// Returns a total number of smart rollup refute operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("sr_refute/count")]
        public async Task<ActionResult<int>> GetSmartRollupRefuteOpsCount([FromQuery] SrRefuteOperationFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.SmartRollupRefuteOpsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, await Operations.GetSmartRollupRefuteOpsCount(filter));

            return this.Bytes(res);
        }
        #endregion

        #region staking
        /// <summary>
        /// Get staking ops
        /// </summary>
        /// <remarks>
        /// Returns a list of staking operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("staking")]
        public async Task<ActionResult<IEnumerable<StakingOperation>>> GetStakingOps(
            [FromQuery] StakingOperationFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection,
            [FromQuery] Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection), ("quote", quote));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, selection.select == null
                    ? await Operations.GetStakingOps(filter, pagination, quote)
                    : new SelectionResponse
                    {
                        Cols = selection.Cols,
                        Rows = await Operations.GetStakingOps(filter, pagination, selection, quote)
                    });

            return this.Bytes(res);
        }

        /// <summary>
        /// Get staking ops count
        /// </summary>
        /// <remarks>
        /// Returns a total number of staking operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("staking/count")]
        public async Task<ActionResult<int>> GetStakingOpsCount([FromQuery] StakingOperationFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.StakingOpsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, await Operations.GetStakingOpsCount(filter));

            return this.Bytes(res);
        }
        #endregion

        #region migrations
        /// <summary>
        /// Get migrations
        /// </summary>
        /// <remarks>
        /// Returns a list of migration operations (synthetic type).
        /// </remarks>
        /// <param name="account">Filters migration operations by account. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="kind">Filters migration operations by kind (`bootstrap`, `activate_delegate`, `airdrop`, `proposal_invoice`, `origination`, `subsidy`, `remove_bigmap_key`).</param>
        /// <param name="balanceChange">Filters migration operations by amount.</param>
        /// <param name="id">Filters migration operations by internal TzKT id.</param>
        /// <param name="level">Filters migration operations by level.</param>
        /// <param name="timestamp">Filters migration operations by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts migrations by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("migrations")]
        public async Task<ActionResult<IEnumerable<MigrationOperation>>> GetMigrations(
            AccountParameter? account,
            MigrationKindParameter? kind,
            Int64Parameter? balanceChange,
            Int64Parameter? id,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (account != null)
            {
                if (account.Eqx != null)
                    return new BadRequest($"{nameof(account)}.eqx", "This parameter doesn't support .eqx mode.");

                if (account.Nex != null)
                    return new BadRequest($"{nameof(account)}.nex", "This parameter doesn't support .nex mode.");

                if (account.Eq == -1 || account.In?.Count == 0 || account.Null == true)
                    return Ok(Enumerable.Empty<MigrationOperation>());
            }

            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("account", account), ("kind", kind), ("balanceChange", balanceChange), ("id", id),
                ("level", level), ("timestamp", timestamp), ("select", select), ("sort", sort), ("offset", offset),
                ("limit", limit), ("micheline", micheline), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetMigrations(null, account, kind, balanceChange, id, level, timestamp, sort, offset, limit, micheline, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetMigrations(account, kind, balanceChange, id, level, timestamp, sort, offset, limit, select.Values[0], micheline, quote);
                else
                    res = await Operations.GetMigrations(account, kind, balanceChange, id, level, timestamp, sort, offset, limit, select.Values, micheline, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetMigrations(account, kind, balanceChange, id, level, timestamp, sort, offset, limit, select.Fields[0], micheline, quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetMigrations(account, kind, balanceChange, id, level, timestamp, sort, offset, limit, select.Fields, micheline, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get migration by id
        /// </summary>
        /// <remarks>
        /// Returns migration operation with specified id.
        /// </remarks>
        /// <param name="id">Operation id</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("migrations/{id:long}")]
        public async Task<ActionResult<MigrationOperation?>> GetMigrationById(
            [Required][Min64(0)] long id,
            MichelineFormat micheline = MichelineFormat.Json, 
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("micheline", micheline), ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetMigration(id, micheline, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get migrations count
        /// </summary>
        /// <remarks>
        /// Returns the total number of migration operations (synthetic type).
        /// </remarks>
        /// <param name="level">Filters migrations by level.</param>
        /// <param name="timestamp">Filters migrations by timestamp.</param>
        /// <returns></returns>
        [HttpGet("migrations/count")]
        public async Task<ActionResult<int>> GetMigrationsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.MigrationOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetMigrationsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region revelation penalties
        /// <summary>
        /// Get revelation penalties
        /// </summary>
        /// <remarks>
        /// Returns a list of revelation penalty operations (synthetic type).
        /// </remarks>
        /// <param name="id">Filters operations by internal TzKT id.</param>
        /// <param name="baker">Filters revelation penalty operations by baker. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters revelation penalty operations by level.</param>
        /// <param name="timestamp">Filters revelation penalty operations by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts revelation penalty operations by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("revelation_penalties")]
        public async Task<ActionResult<IEnumerable<RevelationPenaltyOperation>>> GetRevelationPenalties(
            Int64Parameter? id,
            AccountParameter? baker,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (baker != null)
            {
                if (baker.Eqx != null)
                    return new BadRequest($"{nameof(baker)}.eqx", "This parameter doesn't support .eqx mode.");

                if (baker.Nex != null)
                    return new BadRequest($"{nameof(baker)}.nex", "This parameter doesn't support .nex mode.");

                if (baker.Eq == -1 || baker.In?.Count == 0 || baker.Null == true)
                    return Ok(Enumerable.Empty<RevelationPenaltyOperation>());
            }

            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("id", id), ("baker", baker), ("level", level), ("timestamp", timestamp),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetRevelationPenalties(null, id, baker, level, timestamp, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetRevelationPenalties(id, baker, level, timestamp, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetRevelationPenalties(id, baker, level, timestamp, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetRevelationPenalties(id, baker, level, timestamp, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetRevelationPenalties(id, baker, level, timestamp, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get revelation penalty by id
        /// </summary>
        /// <remarks>
        /// Returns revelation penalty operation with specified id.
        /// </remarks>
        /// <param name="id">Operation id</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("revelation_penalties/{id:long}")]
        public async Task<ActionResult<RevelationPenaltyOperation?>> GetRevelationPenaltyById(
            [Required][Min64(0)] long id,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetRevelationPenalty(id, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get revelation penalties count
        /// </summary>
        /// <remarks>
        /// Returns the total number of revelation penalty operations (synthetic type).
        /// </remarks>
        /// <param name="level">Filters revelation penalty operations by level.</param>
        /// <param name="timestamp">Filters revelation penalty operations by timestamp.</param>
        /// <returns></returns>
        [HttpGet("revelation_penalties/count")]
        public async Task<ActionResult<int>> GetRevelationPenaltiesCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.RevelationPenaltyOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetRevelationPenaltiesCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region baking
        /// <summary>
        /// Get baking
        /// </summary>
        /// <remarks>
        /// Returns a list of baking operations (synthetic type).
        /// </remarks>
        /// <param name="anyof">Filters by any of the specified fields. Example: `anyof.proposer.producer=tz1...`.</param>
        /// <param name="proposer">Filters by block proposer. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="producer">Filters by block producer. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="id">Filters operations by internal TzKT id.</param>
        /// <param name="level">Filters baking operations by level.</param>
        /// <param name="timestamp">Filters baking operations by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts baking operations by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("baking")]
        public async Task<ActionResult<IEnumerable<BakingOperation>>> GetBaking(
            [OpenApiExtensionData("x-tzkt-extension", "anyof-parameter")]
            [OpenApiExtensionData("x-tzkt-anyof-parameter", "proposer,producer")]
            AnyOfParameter? anyof,
            AccountParameter? proposer,
            AccountParameter? producer,
            Int64Parameter? id,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "proposer" && x != "producer"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `proposer`, `producer` fields only.");

                if (anyof.Eq == -1 || anyof.In?.Count == 0 && !anyof.InHasNull)
                    return Ok(Enumerable.Empty<BakingOperation>());
            }
            if (proposer != null)
            {
                if (proposer.Eqx != null)
                    return new BadRequest($"{nameof(proposer)}.eqx", "This parameter doesn't support .eqx mode.");

                if (proposer.Nex != null)
                    return new BadRequest($"{nameof(proposer)}.nex", "This parameter doesn't support .nex mode.");

                if (proposer.Eq == -1 || proposer.In?.Count == 0 && !proposer.InHasNull)
                    return Ok(Enumerable.Empty<BakingOperation>());
            }
            if (producer != null)
            {
                if (producer.Eqx != null)
                    return new BadRequest($"{nameof(producer)}.eqx", "This parameter doesn't support .eqx mode.");

                if (producer.Nex != null)
                    return new BadRequest($"{nameof(producer)}.nex", "This parameter doesn't support .nex mode.");

                if (producer.Eq == -1 || producer.In?.Count == 0 && !producer.InHasNull)
                    return Ok(Enumerable.Empty<BakingOperation>());
            }

            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("anyof", anyof), ("proposer", proposer), ("producer", producer), ("id", id), ("level", level),
                ("timestamp", timestamp), ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetBakings(null, anyof, proposer, producer, id, level, timestamp, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetBakings(anyof, proposer, producer, id, level, timestamp, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetBakings(anyof, proposer, producer, id, level, timestamp, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetBakings(anyof, proposer, producer, id, level, timestamp, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetBakings(anyof, proposer, producer, id, level, timestamp, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get baking by id
        /// </summary>
        /// <remarks>
        /// Returns baking operation with specified id.
        /// </remarks>
        /// <param name="id">Operation id</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("baking/{id:long}")]
        public async Task<ActionResult<BakingOperation?>> GetBakingById(
            [Required][Min64(0)] long id,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetBaking(id, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get baking count
        /// </summary>
        /// <remarks>
        /// Returns the total number of baking operations (synthetic type).
        /// </remarks>
        /// <param name="level">Filters baking operations by level.</param>
        /// <param name="timestamp">Filters baking operations by timestamp.</param>
        /// <returns></returns>
        [HttpGet("baking/count")]
        public async Task<ActionResult<int>> GetBakingCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.BlocksCount - 2);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetBakingsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region endorsing rewards
        /// <summary>
        /// Get endorsing rewards
        /// </summary>
        /// <remarks>
        /// Returns a list of endorsing reward operations (synthetic type).
        /// </remarks>
        /// <param name="id">Filters operations by internal TzKT id.</param>
        /// <param name="baker">Filters by baker. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("endorsing_rewards")]
        public async Task<ActionResult<IEnumerable<EndorsingRewardOperation>>> GetEndorsingRewards(
            Int64Parameter? id,
            AccountParameter? baker,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (baker != null)
            {
                if (baker.Eqx != null)
                    return new BadRequest($"{nameof(baker)}.eqx", "This parameter doesn't support .eqx mode.");

                if (baker.Nex != null)
                    return new BadRequest($"{nameof(baker)}.nex", "This parameter doesn't support .nex mode.");

                if (baker.Eq == -1 || baker.In?.Count == 0 || baker.Null == true)
                    return Ok(Enumerable.Empty<EndorsingRewardOperation>());
            }

            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("id", id), ("baker", baker), ("level", level), ("timestamp", timestamp),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetEndorsingRewards(null, id, baker, level, timestamp, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetEndorsingRewards(id, baker, level, timestamp, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetEndorsingRewards(id, baker, level, timestamp, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetEndorsingRewards(id, baker, level, timestamp, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetEndorsingRewards(id, baker, level, timestamp, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get endorsing reward by id
        /// </summary>
        /// <remarks>
        /// Returns endorsing reward operation with specified id.
        /// </remarks>
        /// <param name="id">Operation id</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("endorsing_rewards/{id:long}")]
        public async Task<ActionResult<EndorsingRewardOperation?>> GetEndorsingRewardById(
            [Required][Min64(0)] long id,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetEndorsingReward(id, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get endorsing rewards count
        /// </summary>
        /// <remarks>
        /// Returns the total number of endorsing reward operations (synthetic type).
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("endorsing_rewards/count")]
        public async Task<ActionResult<int>> GetEndorsingRewardsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.EndorsingRewardOpsCount);
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetEndorsingRewardsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region dal attestation rewards
        /// <summary>
        /// Get dal attestation rewards
        /// </summary>
        /// <remarks>
        /// Returns a list of dal attestation reward operations (synthetic type).
        /// </remarks>
        /// <param name="id">Filters operations by internal TzKT id.</param>
        /// <param name="baker">Filters by baker. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("dal_attestation_reward")]
        public async Task<ActionResult<IEnumerable<DalAttestationRewardOperation>>> GetDalAttestationRewards(
            Int64Parameter? id,
            AccountParameter? baker,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SelectParameter? select,
            SortParameter? sort,
            OffsetParameter? offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (baker != null)
            {
                if (baker.Eqx != null)
                    return new BadRequest($"{nameof(baker)}.eqx", "This parameter doesn't support .eqx mode.");

                if (baker.Nex != null)
                    return new BadRequest($"{nameof(baker)}.nex", "This parameter doesn't support .nex mode.");

                if (baker.Eq == -1 || baker.In?.Count == 0 || baker.Null == true)
                    return Ok(Enumerable.Empty<DalAttestationRewardOperation>());
            }

            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("id", id), ("baker", baker), ("level", level), ("timestamp", timestamp),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Operations.GetDalAttestationRewards(null, id, baker, level, timestamp, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Operations.GetDalAttestationRewards(id, baker, level, timestamp, sort, offset, limit, select.Values[0], quote);
                else
                    res = await Operations.GetDalAttestationRewards(id, baker, level, timestamp, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields!.Length == 1)
                    res = await Operations.GetDalAttestationRewards(id, baker, level, timestamp, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetDalAttestationRewards(id, baker, level, timestamp, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get dal attestation reward by id
        /// </summary>
        /// <remarks>
        /// Returns dal attestation reward operation with specified id.
        /// </remarks>
        /// <param name="id">Operation id</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("dal_attestation_reward/{id:long}")]
        public async Task<ActionResult<DalAttestationRewardOperation?>> GetDalAttestationRewardById(
            [Required][Min64(0)] long id,
            Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetDalAttestationReward(id, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get dal attestation rewards count
        /// </summary>
        /// <remarks>
        /// Returns the total number of dal attestation reward operations (synthetic type).
        /// </remarks>
        /// <param name="level">Filters by level.</param>
        /// <param name="timestamp">Filters by timestamp.</param>
        /// <returns></returns>
        [HttpGet("dal_attestation_reward/count")]
        public async Task<ActionResult<int>> GetDalAttestationRewardsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            if (level == null && timestamp == null)
                return Ok(State.Current.DalAttestationRewardOpsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("level", level), ("timestamp", timestamp));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Operations.GetDalAttestationRewardsCount(level, timestamp);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }
        #endregion

        #region autostaking
        /// <summary>
        /// Get autostaking ops
        /// </summary>
        /// <remarks>
        /// Returns a list of autostaking operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("autostaking")]
        public async Task<ActionResult<IEnumerable<AutostakingOperation>>> GetAutostakingOps(
            [FromQuery] AutostakingOperationFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection,
            [FromQuery] Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection), ("quote", quote));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, selection.select == null
                    ? await Operations.GetAutostakingOps(filter, pagination, quote)
                    : new SelectionResponse
                    {
                        Cols = selection.Cols,
                        Rows = await Operations.GetAutostakingOps(filter, pagination, selection, quote)
                    });

            return this.Bytes(res);
        }

        /// <summary>
        /// Get autostaking ops count
        /// </summary>
        /// <remarks>
        /// Returns a total number of autostaking operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("autostaking/count")]
        public async Task<ActionResult<int>> GetAutostakingOpsCount([FromQuery] AutostakingOperationFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.AutostakingOpsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, await Operations.GetAutostakingOpsCount(filter));

            return this.Bytes(res);
        }
        #endregion

        #region set delegate parameters
        /// <summary>
        /// Get set_delegate_parameters ops
        /// </summary>
        /// <remarks>
        /// Returns a list of set_delegate_parameters operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("set_delegate_parameters")]
        public async Task<ActionResult<IEnumerable<SetDelegateParametersOperation>>> GetSetDelegateParametersOps(
            [FromQuery] ManagerOperationFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection,
            [FromQuery] Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection), ("quote", quote));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, selection.select == null
                    ? await Operations.GetSetDelegateParametersOps(filter, pagination, quote)
                    : new SelectionResponse
                    {
                        Cols = selection.Cols,
                        Rows = await Operations.GetSetDelegateParametersOps(filter, pagination, selection, quote)
                    });

            return this.Bytes(res);
        }

        /// <summary>
        /// Get set_delegate_parameters ops count
        /// </summary>
        /// <remarks>
        /// Returns a total number of set_delegate_parameters operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("set_delegate_parameters/count")]
        public async Task<ActionResult<int>> GetSetDelegateParametersOpsCount([FromQuery] ManagerOperationFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.SetDelegateParametersOpsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, await Operations.GetSetDelegateParametersOpsCount(filter));

            return this.Bytes(res);
        }
        #endregion

        #region dal publish commitment
        /// <summary>
        /// Get dal_publish_commitment ops
        /// </summary>
        /// <remarks>
        /// Returns a list of dal_publish_commitment operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("dal_publish_commitment")]
        public async Task<ActionResult<IEnumerable<DalPublishCommitmentOperation>>> GetDalPublishCommitmentOps(
            [FromQuery] ManagerOperationFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection,
            [FromQuery] Symbols quote = Symbols.None)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection), ("quote", quote));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, selection.select == null
                    ? await Operations.GetDalPublishCommitmentOps(filter, pagination, quote)
                    : new SelectionResponse
                    {
                        Cols = selection.Cols,
                        Rows = await Operations.GetDalPublishCommitmentOps(filter, pagination, selection, quote)
                    });

            return this.Bytes(res);
        }

        /// <summary>
        /// Get dal_publish_commitment ops count
        /// </summary>
        /// <remarks>
        /// Returns a total number of dal_publish_commitment operations.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("dal_publish_commitment/count")]
        public async Task<ActionResult<int>> GetDalPublishCommitmentOpsCount([FromQuery] ManagerOperationFilter filter)
        {
            if (filter.Empty)
                return Ok(State.Current.DalPublishCommitmentOpsCount);

            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (!ResponseCache.TryGet(query, out var res))
                res = ResponseCache.Set(query, await Operations.GetDalPublishCommitmentOpsCount(filter));

            return this.Bytes(res);
        }
        #endregion
    }
}
