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
    [Route("v1/operations")]
    public class OperationsController : ControllerBase
    {
        private readonly OperationRepository Operations;
        private readonly StateCache State;

        public OperationsController(OperationRepository operations, StateCache state)
        {
            Operations = operations;
            State = state;
        }

        #region operations
        /// <summary>
        /// Get operations by hash
        /// </summary>
        /// <remarks>
        /// Returns a list of operations with the specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{hash}")]
        public Task<IEnumerable<Operation>> GetByHash(
            [Required][OpHash] string hash,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            return Operations.Get(hash, micheline, quote);
        }

        /// <summary>
        /// Get operations by hash and counter
        /// </summary>
        /// <remarks>
        /// Returns a list of operations with the specified hash and counter.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="counter">Operation counter</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{hash}/{counter}")]
        public Task<IEnumerable<Operation>> GetByHashCounter(
            [Required][OpHash] string hash,
            [Min(0)] int counter,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            return Operations.Get(hash, counter, micheline, quote);
        }

        /// <summary>
        /// Get operations by hash, counter and nonce
        /// </summary>
        /// <remarks>
        /// Returns an internal operations with the specified hash, counter and nonce.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="counter">Operation counter</param>
        /// <param name="nonce">Operation nonce (internal)</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{hash}/{counter}/{nonce}")]
        public Task<IEnumerable<Operation>> GetByHashCounterNonce(
            [Required][OpHash] string hash,
            [Min(0)] int counter,
            [Min(0)] int nonce,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            return Operations.Get(hash, counter, nonce, micheline, quote);
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
            AccountParameter @delegate,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
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

            if (select == null)
                return Ok(await Operations.GetEndorsements(@delegate, level, timestamp, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetEndorsements(@delegate, level, timestamp, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetEndorsements(@delegate, level, timestamp, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetEndorsements(@delegate, level, timestamp, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetEndorsements(@delegate, level, timestamp, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
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
        public Task<IEnumerable<EndorsementOperation>> GetEndorsementByHash([Required][OpHash] string hash, Symbols quote = Symbols.None)
        {
            return Operations.GetEndorsements(hash, quote);
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
        public Task<int> GetEndorsementsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.EndorsementOpsCount);

            return Operations.GetEndorsementsCount(level, timestamp);
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
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts ballots by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("ballots")]
        public async Task<ActionResult<IEnumerable<BallotOperation>>> GetBallots(
            AccountParameter @delegate,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter epoch,
            Int32Parameter period,
            ProtocolParameter proposal,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
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

            if (select == null)
                return Ok(await Operations.GetBallots(@delegate, level, timestamp, epoch, period, proposal, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetBallots(@delegate, level, timestamp, epoch, period, proposal, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetBallots(@delegate, level, timestamp, epoch, period, proposal, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetBallots(@delegate, level, timestamp, epoch, period, proposal, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetBallots(@delegate, level, timestamp, epoch, period, proposal, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
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
        public Task<IEnumerable<BallotOperation>> GetBallotByHash([Required][OpHash] string hash, Symbols quote = Symbols.None)
        {
            return Operations.GetBallots(hash, quote);
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
        public Task<int> GetBallotsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.BallotOpsCount);

            return Operations.GetBallotsCount(level, timestamp);
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
            AccountParameter @delegate,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter epoch,
            Int32Parameter period,
            ProtocolParameter proposal,
            BoolParameter duplicated,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
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

            if (select == null)
                return Ok(await Operations.GetProposals(@delegate, level, timestamp, epoch, period, proposal, duplicated, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetProposals(@delegate, level, timestamp, epoch, period, proposal, duplicated, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetProposals(@delegate, level, timestamp, epoch, period, proposal, duplicated, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetProposals(@delegate, level, timestamp, epoch, period, proposal, duplicated, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetProposals(@delegate, level, timestamp, epoch, period, proposal, duplicated, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
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
        public Task<IEnumerable<ProposalOperation>> GetProposalByHash([Required][OpHash] string hash, Symbols quote = Symbols.None)
        {
            return Operations.GetProposals(hash, quote);
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
        public Task<int> GetProposalsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.ProposalOpsCount);

            return Operations.GetProposalsCount(level, timestamp);
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
            AccountParameter account,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
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

            if (select == null)
                return Ok(await Operations.GetActivations(account, level, timestamp, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetActivations(account, level, timestamp, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetActivations(account, level, timestamp, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetActivations(account, level, timestamp, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetActivations(account, level, timestamp, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
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
        public Task<IEnumerable<ActivationOperation>> GetActivationByHash([Required][OpHash] string hash, Symbols quote = Symbols.None)
        {
            return Operations.GetActivations(hash, quote);
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
        public Task<int> GetActivationsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.ActivationOpsCount);

            return Operations.GetActivationsCount(level, timestamp);
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
            AnyOfParameter anyof,
            AccountParameter accuser,
            AccountParameter offender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "accuser" && x != "offender"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `accuser`, `offender` fields only.");

                if (anyof.Value == -1)
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

            if (select == null)
                return Ok(await Operations.GetDoubleBakings(anyof, accuser, offender, level, timestamp, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetDoubleBakings(anyof, accuser, offender, level, timestamp, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetDoubleBakings(anyof, accuser, offender, level, timestamp, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetDoubleBakings(anyof, accuser, offender, level, timestamp, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetDoubleBakings(anyof, accuser, offender, level, timestamp, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
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
        public Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakingByHash([Required][OpHash] string hash, Symbols quote = Symbols.None)
        {
            return Operations.GetDoubleBakings(hash, quote);
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
        public Task<int> GetDoubleBakingCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.DoubleBakingOpsCount);

            return Operations.GetDoubleBakingsCount(level, timestamp);
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
            AnyOfParameter anyof,
            AccountParameter accuser,
            AccountParameter offender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "accuser" && x != "offender"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `accuser`, `offender` fields only.");

                if (anyof.Value == -1)
                    return Ok(Enumerable.Empty<DoubleEndorsingOperation>());
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

            if (select == null)
                return Ok(await Operations.GetDoubleEndorsings(anyof, accuser, offender, level, timestamp, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetDoubleEndorsings(anyof, accuser, offender, level, timestamp, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetDoubleEndorsings(anyof, accuser, offender, level, timestamp, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetDoubleEndorsings(anyof, accuser, offender, level, timestamp, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetDoubleEndorsings(anyof, accuser, offender, level, timestamp, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
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
        public Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsingByHash([Required][OpHash] string hash, Symbols quote = Symbols.None)
        {
            return Operations.GetDoubleEndorsings(hash, quote);
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
        public Task<int> GetDoubleEndorsingCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.DoubleEndorsingOpsCount);

            return Operations.GetDoubleEndorsingsCount(level, timestamp);
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
        /// <param name="timestamp">Filters nonce revelation operations by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts nonce revelation operations by specified field. Supported fields: `id` (default), `level`, `revealedLevel`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("nonce_revelations")]
        public async Task<ActionResult<IEnumerable<NonceRevelationOperation>>> GetNonceRevelations(
            AnyOfParameter anyof,
            AccountParameter baker,
            AccountParameter sender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "baker" && x != "sender"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `baker`, `sender` fields only.");

                if (anyof.Value == -1)
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

            if (select == null)
                return Ok(await Operations.GetNonceRevelations(anyof, baker, sender, level, timestamp, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetNonceRevelations(anyof, baker, sender, level, timestamp, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetNonceRevelations(anyof, baker, sender, level, timestamp, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetNonceRevelations(anyof, baker, sender, level, timestamp, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetNonceRevelations(anyof, baker, sender, level, timestamp, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
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
        public Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelationByHash([Required][OpHash] string hash, Symbols quote = Symbols.None)
        {
            return Operations.GetNonceRevelations(hash, quote);
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
        public Task<int> GetNonceRevelationsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.NonceRevelationOpsCount);

            return Operations.GetNonceRevelationsCount(level, timestamp);
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
        /// <param name="level">Filters delegations by level.</param>
        /// <param name="timestamp">Filters delegations by timestamp.</param>
        /// <param name="status">Filters delegations by operation status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts delegations by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `bakerFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("delegations")]
        public async Task<ActionResult<IEnumerable<DelegationOperation>>> GetDelegations(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            Int32Parameter level,
            DateTimeParameter timestamp,
            OperationStatusParameter status,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "initiator" && x != "sender" && x != "prevDelegate" && x != "newDelegate"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `initiator`, `sender`, `prevDelegate`, `newDelegate` fields only.");

                if (anyof.Value == -1)
                    return Ok(Enumerable.Empty<DelegationOperation>());
            }

            if (initiator != null)
            {
                if (initiator.Eqx != null && initiator.Eqx != "prevDelegate" && initiator.Eqx != "newDelegate")
                    return new BadRequest($"{nameof(initiator)}.eqx", "The 'initiator' field can be compared with the 'prevDelegate' or 'newDelegate' field only.");

                if (initiator.Nex != null && initiator.Nex != "prevDelegate" && initiator.Nex != "newDelegate")
                    return new BadRequest($"{nameof(initiator)}.nex", "The 'initiator' field can be compared with the 'prevDelegate' or 'newDelegate' field only.");

                if (initiator.Eq == -1 || initiator.In?.Count == 0)
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

                if (prevDelegate.Eq == -1 || prevDelegate.In?.Count == 0)
                    return Ok(Enumerable.Empty<DelegationOperation>());
            }

            if (newDelegate != null)
            {
                if (newDelegate.Eqx != null && newDelegate.Eqx != "initiator" && newDelegate.Eqx != "sender" && newDelegate.Eqx != "prevDelegate")
                    return new BadRequest($"{nameof(newDelegate)}.eqx", "The 'newDelegate' field can be compared with the 'initiator', 'sender' or 'prevDelegate' fields only.");

                if (newDelegate.Nex != null && newDelegate.Nex != "initiator" && newDelegate.Nex != "sender" && newDelegate.Nex != "prevDelegate")
                    return new BadRequest($"{nameof(newDelegate)}.nex", "The 'newDelegate' field can be compared with the 'initiator', 'sender' or 'prevDelegate' fields only.");

                if (newDelegate.Eq == -1 || newDelegate.In?.Count == 0)
                    return Ok(Enumerable.Empty<DelegationOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "bakerFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetDelegations(anyof, initiator, sender, prevDelegate, newDelegate, level, timestamp, status, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetDelegations(anyof, initiator, sender, prevDelegate, newDelegate, level, timestamp, status, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetDelegations(anyof, initiator, sender, prevDelegate, newDelegate, level, timestamp, status, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetDelegations(anyof, initiator, sender, prevDelegate, newDelegate, level, timestamp, status, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetDelegations(anyof, initiator, sender, prevDelegate, newDelegate, level, timestamp, status, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
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
        public Task<IEnumerable<DelegationOperation>> GetDelegationByHash([Required][OpHash] string hash, Symbols quote = Symbols.None)
        {
            return Operations.GetDelegations(hash, quote);
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
        public Task<int> GetDelegationsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.DelegationOpsCount);

            return Operations.GetDelegationsCount(level, timestamp);
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
        /// <param name="initiator">Filters origination operations by initiator. Allowed fields for `.eqx` mode: `contractManager`, `contractDelegate`.</param>
        /// <param name="sender">Filters origination operations by sender. Allowed fields for `.eqx` mode: `contractManager`, `contractDelegate`.</param>
        /// <param name="contractManager">Filters origination operations by manager. Allowed fields for `.eqx` mode: `initiator`, `sender`, `contractDelegate`.</param>
        /// <param name="contractDelegate">Filters origination operations by delegate. Allowed fields for `.eqx` mode: `initiator`, `sender`, `contractManager`.</param>
        /// <param name="originatedContract">Filters origination operations by originated contract. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="typeHash">Filters origination operations by 32-bit hash of originated contract parameter and storage types (helpful for searching originations of similar contracts)</param>
        /// <param name="codeHash">Filters origination operations by 32-bit hash of originated contract code (helpful for searching originations of same contracts)</param>
        /// <param name="level">Filters origination operations by level.</param>
        /// <param name="timestamp">Filters origination operations by timestamp.</param>
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
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter contractManager,
            AccountParameter contractDelegate,
            AccountParameter originatedContract,
            Int32Parameter typeHash,
            Int32Parameter codeHash,
            Int32Parameter level,
            DateTimeParameter timestamp,
            OperationStatusParameter status,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            #region validates
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "initiator" && x != "sender" && x != "contractManager" && x != "contractDelegate" && x != "originatedContract"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `initiator`, `sender`, `contractManager`, `contractDelegate`, `originatedContract` fields only.");

                if (anyof.Value == -1)
                    return Ok(Enumerable.Empty<OriginationOperation>());
            }

            if (initiator != null)
            {
                if (initiator.Eqx != null && initiator.Eqx != "contractManager" && initiator.Eqx != "contractDelegate")
                    return new BadRequest($"{nameof(initiator)}.eqx", "The 'initiator' field can be compared with the 'contractManager' or 'contractDelegate' field only.");

                if (initiator.Nex != null && initiator.Nex != "contractManager" && initiator.Nex != "contractDelegate")
                    return new BadRequest($"{nameof(initiator)}.nex", "The 'initiator' field can be compared with the 'contractManager' or 'contractDelegate' field only.");

                if (initiator.Eq == -1 || initiator.In?.Count == 0)
                    return Ok(Enumerable.Empty<OriginationOperation>());
            }

            if (sender != null)
            {
                if (sender.Eqx != null && sender.Eqx != "contractManager" && sender.Eqx != "contractDelegate")
                    return new BadRequest($"{nameof(sender)}.eqx", "The 'sender' field can be compared with the 'contractManager' or 'contractDelegate' field only.");

                if (sender.Nex != null && sender.Nex != "contractManager" && sender.Nex != "contractDelegate")
                    return new BadRequest($"{nameof(sender)}.nex", "The 'sender' field can be compared with the 'contractManager' or 'contractDelegate' field only.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<OriginationOperation>());
            }

            if (contractManager != null)
            {
                if (contractManager.Eqx != null && contractManager.Eqx != "initiator" && contractManager.Eqx != "sender" && contractManager.Eqx != "contractDelegate")
                    return new BadRequest($"{nameof(contractManager)}.eqx", "The 'contractManager' field can be compared with the 'initiator', 'sender' or 'contractDelegate' field only.");

                if (contractManager.Nex != null && contractManager.Nex != "initiator" && contractManager.Nex != "sender" && contractManager.Nex != "contractDelegate")
                    return new BadRequest($"{nameof(contractManager)}.nex", "The 'contractManager' field can be compared with the 'initiator', 'sender' or 'contractDelegate' field only.");

                if (contractManager.Eq == -1 || contractManager.In?.Count == 0)
                    return Ok(Enumerable.Empty<OriginationOperation>());
            }

            if (contractDelegate != null)
            {
                if (contractDelegate.Eqx != null && contractDelegate.Eqx != "initiator" && contractDelegate.Eqx != "sender" && contractDelegate.Eqx != "contractManager")
                    return new BadRequest($"{nameof(contractDelegate)}.eqx", "The 'contractDelegate' field can be compared with the 'initiator', 'sender' or 'contractManager' field only.");

                if (contractDelegate.Nex != null && contractDelegate.Nex != "initiator" && contractDelegate.Nex != "sender" && contractDelegate.Nex != "contractManager")
                    return new BadRequest($"{nameof(contractDelegate)}.nex", "The 'contractDelegate' field can be compared with the 'initiator', 'sender' or 'contractManager' field only.");

                if (contractDelegate.Eq == -1 || contractDelegate.In?.Count == 0)
                    return Ok(Enumerable.Empty<OriginationOperation>());
            }

            if (originatedContract != null)
            {
                if (originatedContract.Eqx != null)
                    return new BadRequest($"{nameof(originatedContract)}.eqx", "This parameter doesn't support .eqx mode.");

                if (originatedContract.Nex != null)
                    return new BadRequest($"{nameof(originatedContract)}.nex", "This parameter doesn't support .nex mode.");

                if (originatedContract.Eq == -1 || originatedContract.In?.Count == 0)
                    return Ok(Enumerable.Empty<OriginationOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "storageUsed", "bakerFee", "storageFee", "allocationFee", "contractBalance"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetOriginations(anyof, initiator, sender, contractManager, contractDelegate, originatedContract, typeHash, codeHash, level, timestamp, status, sort, offset, limit, micheline, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetOriginations(anyof, initiator, sender, contractManager, contractDelegate, originatedContract, typeHash, codeHash, level, timestamp, status, sort, offset, limit, select.Values[0], micheline, quote));
                else
                    return Ok(await Operations.GetOriginations(anyof, initiator, sender, contractManager, contractDelegate, originatedContract, typeHash, codeHash, level, timestamp, status, sort, offset, limit, select.Values, micheline, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetOriginations(anyof, initiator, sender, contractManager, contractDelegate, originatedContract, typeHash, codeHash, level, timestamp, status, sort, offset, limit, select.Fields[0], micheline, quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetOriginations(anyof, initiator, sender, contractManager, contractDelegate, originatedContract, typeHash, codeHash, level, timestamp, status, sort, offset, limit, select.Fields, micheline, quote)
                    });
                }
            }
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
        public Task<IEnumerable<OriginationOperation>> GetOriginationByHash(
            [Required][OpHash] string hash,
            MichelineFormat micheline,
            Symbols quote = Symbols.None)
        {
            return Operations.GetOriginations(hash, micheline, quote);
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
        public Task<int> GetOriginationsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.OriginationOpsCount);

            return Operations.GetOriginationsCount(level, timestamp);
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
        /// <param name="amount">Filters transactions by amount (microtez).</param>
        /// <param name="level">Filters transactions by level.</param>
        /// <param name="timestamp">Filters transactions by timestamp.</param>
        /// <param name="hasInternals">Filters transactions by presence of internal operations.</param>
        /// <param name="entrypoint">Filters transactions by entrypoint called on the target contract.</param>
        /// <param name="parameter">Filters transactions by parameter value. Note, this query parameter supports the following format: `?parameter{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?parameter.token_id=...` or `?parameter.sigs.0.ne=...`.</param>
        /// <param name="parameters">**DEPRECATED**. Use `entrypoint` and `parameter` instead.</param>
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
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            Int64Parameter amount,
            Int32Parameter level,
            DateTimeParameter timestamp,
            StringParameter entrypoint,
            JsonParameter parameter,
            StringParameter parameters,
            BoolParameter hasInternals,
            OperationStatusParameter status,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None) 
        {
            #region validate
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "initiator" && x != "sender" && x != "target"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `initiator`, `sender`, `target` fields only.");

                if (anyof.Value == -1)
                    return Ok(Enumerable.Empty<TransactionOperation>());
            }

            if (initiator != null)
            {
                if (initiator.Eqx != null && initiator.Eqx != "target")
                    return new BadRequest($"{nameof(initiator)}.eqx", "The 'initiator' field can be compared with the 'target' field only.");

                if (initiator.Nex != null && initiator.Nex != "target")
                    return new BadRequest($"{nameof(initiator)}.nex", "The 'initiator' field can be compared with the 'target' field only.");

                if (initiator.Eq == -1 || initiator.In?.Count == 0)
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

                if (target.Eq == -1 || target.In?.Count == 0)
                    return Ok(Enumerable.Empty<TransactionOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "storageUsed", "bakerFee", "storageFee", "allocationFee", "amount"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetTransactions(anyof, initiator, sender, target, amount, level, timestamp, entrypoint, parameter, parameters, hasInternals, status, sort, offset, limit, micheline, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetTransactions(anyof, initiator, sender, target, amount, level, timestamp, entrypoint, parameter, hasInternals, status, sort, offset, limit, select.Values[0], micheline, quote));
                else
                    return Ok(await Operations.GetTransactions(anyof, initiator, sender, target, amount, level, timestamp, entrypoint, parameter, hasInternals, status, sort, offset, limit, select.Values, micheline, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetTransactions(anyof, initiator, sender, target, amount, level, timestamp, entrypoint, parameter, hasInternals, status, sort, offset, limit, select.Fields[0], micheline, quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetTransactions(anyof, initiator, sender, target, amount, level, timestamp, entrypoint, parameter, hasInternals, status, sort, offset, limit, select.Fields, micheline, quote)
                    });
                }
            }
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
        public Task<IEnumerable<TransactionOperation>> GetTransactionByHash(
            [Required][OpHash] string hash,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            return Operations.GetTransactions(hash, micheline, quote);
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
        public Task<IEnumerable<TransactionOperation>> GetTransactionByHashCounter(
            [Required][OpHash] string hash,
            [Min(0)] int counter,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            return Operations.GetTransactions(hash, counter, micheline, quote);
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
        public Task<IEnumerable<TransactionOperation>> GetTransactionByHashCounterNonce(
            [Required][OpHash] string hash,
            [Min(0)] int counter,
            [Min(0)] int nonce,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            return Operations.GetTransactions(hash, counter, nonce, micheline, quote);
        }

        /// <summary>
        /// Get transactions count
        /// </summary>
        /// <remarks>
        /// Returns the total number of transaction operations.
        /// </remarks>
        /// <param name="level">Filters transactions by level.</param>
        /// <param name="timestamp">Filters transactions by timestamp.</param>
        /// <param name="status">Filters transactions by operation status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <returns></returns>
        [HttpGet("transactions/count")]
        public Task<int> GetTransactionsCount(
            Int32Parameter level,
            DateTimeParameter timestamp,
            OperationStatusParameter status)
        {
            if (level == null && timestamp == null && status == null)
                return Task.FromResult(State.Current.TransactionOpsCount);

            return Operations.GetTransactionsCount(level, timestamp, status);
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
            AccountParameter sender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            OperationStatusParameter status,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
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

            if (select == null)
                return Ok(await Operations.GetReveals(sender, level, timestamp, status, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetReveals(sender, level, timestamp, status, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetReveals(sender, level, timestamp, status, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetReveals(sender, level, timestamp, status, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetReveals(sender, level, timestamp, status, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
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
        public Task<IEnumerable<RevealOperation>> GetRevealByHash([Required][OpHash] string hash, Symbols quote = Symbols.None)
        {
            return Operations.GetReveals(hash, quote);
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
        public Task<int> GetRevealsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.RevealOpsCount);

            return Operations.GetRevealsCount(level, timestamp);
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
        /// <param name="kind">Filters migration operations by kind (`bootstrap`, `activate_delegate`, `airdrop`, `proposal_invoice`).</param>
        /// <param name="balanceChange">Filters migration operations by amount.</param>
        /// <param name="level">Filters migration operations by level.</param>
        /// <param name="timestamp">Filters migration operations by timestamp.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts migrations by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <returns></returns>
        [HttpGet("migrations")]
        public async Task<ActionResult<IEnumerable<MigrationOperation>>> GetMigrations(
            AccountParameter account,
            MigrationKindParameter kind,
            Int64Parameter balanceChange,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
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

            if (select == null)
                return Ok(await Operations.GetMigrations(account, kind, balanceChange, level, timestamp, sort, offset, limit, micheline, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetMigrations(account, kind, balanceChange, level, timestamp, sort, offset, limit, select.Values[0], micheline, quote));
                else
                    return Ok(await Operations.GetMigrations(account, kind, balanceChange, level, timestamp, sort, offset, limit, select.Values, micheline, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetMigrations(account, kind, balanceChange, level, timestamp, sort, offset, limit, select.Fields[0], micheline, quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetMigrations(account, kind, balanceChange, level, timestamp, sort, offset, limit, select.Fields, micheline, quote)
                    });
                }
            }
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
        public Task<int> GetMigrationsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.MigrationOpsCount);

            return Operations.GetMigrationsCount(level, timestamp);
        }
        #endregion

        #region revelation penalties
        /// <summary>
        /// Get revelation penalties
        /// </summary>
        /// <remarks>
        /// Returns a list of revelation penalty operations (synthetic type).
        /// </remarks>
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
            AccountParameter baker,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
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

            if (select == null)
                return Ok(await Operations.GetRevelationPenalties(baker, level, timestamp, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetRevelationPenalties(baker, level, timestamp, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetRevelationPenalties(baker, level, timestamp, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetRevelationPenalties(baker, level, timestamp, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetRevelationPenalties(baker, level, timestamp, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
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
        public Task<int> GetRevelationPenaltiesCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.RevelationPenaltyOpsCount);

            return Operations.GetRevelationPenaltiesCount(level, timestamp);
        }
        #endregion

        #region baking
        /// <summary>
        /// Get baking
        /// </summary>
        /// <remarks>
        /// Returns a list of baking operations (synthetic type).
        /// </remarks>
        /// <param name="baker">Filters baking operations by baker. Allowed fields for `.eqx` mode: none.</param>
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
            AccountParameter baker,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
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
                    return Ok(Enumerable.Empty<BakingOperation>());
            }

            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetBakings(baker, level, timestamp, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetBakings(baker, level, timestamp, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Operations.GetBakings(baker, level, timestamp, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetBakings(baker, level, timestamp, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetBakings(baker, level, timestamp, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
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
        public Task<int> GetBakingCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            if (level == null && timestamp == null)
                return Task.FromResult(State.Current.BlocksCount - 2);

            return Operations.GetBakingsCount(level, timestamp);
        }
        #endregion
    }
}
