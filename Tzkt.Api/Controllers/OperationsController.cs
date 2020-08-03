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
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{hash}")]
        public Task<IEnumerable<Operation>> GetByHash([OpHash] string hash, Symbols quotes = Symbols.None)
        {
            return Operations.Get(hash, quotes);
        }

        /// <summary>
        /// Get operations by hash and counter
        /// </summary>
        /// <remarks>
        /// Returns a list of operations with the specified hash and counter.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="counter">Operation counter</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{hash}/{counter}")]
        public Task<IEnumerable<Operation>> GetByHashCounter([OpHash] string hash, [Min(0)] int counter, Symbols quotes = Symbols.None)
        {
            return Operations.Get(hash, counter, quotes);
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
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{hash}/{counter}/{nonce}")]
        public Task<IEnumerable<Operation>> GetByHashCounterNonce([OpHash] string hash, [Min(0)] int counter, [Min(0)] int nonce, Symbols quotes = Symbols.None)
        {
            return Operations.Get(hash, counter, nonce, quotes);
        }
        #endregion

        #region endorsements
        /// <summary>
        /// Get endorsements
        /// </summary>
        /// <remarks>
        /// Returns a list of endorsement operations.
        /// </remarks>
        /// <param name="level">Filters endorsements by level.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts endorsements by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("endorsements")]
        public async Task<ActionResult<IEnumerable<EndorsementOperation>>> GetEndorsements(
            Int32Parameter level,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quotes = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetEndorsements(level, sort, offset, limit, quotes));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetEndorsements(level, sort, offset, limit, select.Values[0], quotes));
                else
                    return Ok(await Operations.GetEndorsements(level, sort, offset, limit, select.Values, quotes));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetEndorsements(level, sort, offset, limit, select.Fields[0], quotes));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetEndorsements(level, sort, offset, limit, select.Fields, quotes)
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
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("endorsements/{hash}")]
        public Task<IEnumerable<EndorsementOperation>> GetEndorsementByHash([OpHash] string hash, Symbols quotes = Symbols.None)
        {
            return Operations.GetEndorsements(hash, quotes);
        }

        /// <summary>
        /// Get endorsements count
        /// </summary>
        /// <remarks>
        /// Returns the total number of endorsement operations.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("endorsements/count")]
        public Task<int> GetEndorsementsCount()
        {
            return Task.FromResult(State.GetState().EndorsementOpsCount);
        }
        #endregion

        #region ballots
        /// <summary>
        /// Get ballots
        /// </summary>
        /// <remarks>
        /// Returns a list of ballot operations.
        /// </remarks>
        /// <param name="level">Filters ballots by level.</param>
        /// <param name="period">Filters ballots by voting period id.</param>
        /// <param name="proposal">Filters ballots by proposal hash.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts ballots by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("ballots")]
        public async Task<ActionResult<IEnumerable<BallotOperation>>> GetBallots(
            Int32Parameter level,
            Int32Parameter period,
            ProtocolParameter proposal,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quotes = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetBallots(level, period, proposal, sort, offset, limit, quotes));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetBallots(level, period, proposal, sort, offset, limit, select.Values[0], quotes));
                else
                    return Ok(await Operations.GetBallots(level, period, proposal, sort, offset, limit, select.Values, quotes));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetBallots(level, period, proposal, sort, offset, limit, select.Fields[0], quotes));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetBallots(level, period, proposal, sort, offset, limit, select.Fields, quotes)
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
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("ballots/{hash}")]
        public Task<IEnumerable<BallotOperation>> GetBallotByHash([OpHash] string hash, Symbols quotes = Symbols.None)
        {
            return Operations.GetBallots(hash, quotes);
        }

        /// <summary>
        /// Get ballots count
        /// </summary>
        /// <remarks>
        /// Returns the total number of ballot operations.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("ballots/count")]
        public Task<int> GetBallotsCount()
        {
            return Task.FromResult(State.GetState().BallotOpsCount);
        }
        #endregion

        #region proposals
        /// <summary>
        /// Get proposals
        /// </summary>
        /// <remarks>
        /// Returns a list of proposal operations.
        /// </remarks>
        /// <param name="level">Filters proposal operations by level.</param>
        /// <param name="period">Filters proposal operations by voting period id.</param>
        /// <param name="proposal">Filters proposal operations by proposal hash.</param>
        /// <param name="duplicated">Specify whether to include or exclude duplicates, which didn't actually upvote a proposal.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts proposal operations by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("proposals")]
        public async Task<ActionResult<IEnumerable<ProposalOperation>>> GetProposals(
            Int32Parameter level,
            Int32Parameter period,
            ProtocolParameter proposal,
            BoolParameter duplicated,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quotes = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetProposals(level, period, proposal, duplicated, sort, offset, limit, quotes));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetProposals(level, period, proposal, duplicated, sort, offset, limit, select.Values[0], quotes));
                else
                    return Ok(await Operations.GetProposals(level, period, proposal, duplicated, sort, offset, limit, select.Values, quotes));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetProposals(level, period, proposal, duplicated, sort, offset, limit, select.Fields[0], quotes));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetProposals(level, period, proposal, duplicated, sort, offset, limit, select.Fields, quotes)
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
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("proposals/{hash}")]
        public Task<IEnumerable<ProposalOperation>> GetProposalByHash([OpHash] string hash, Symbols quotes = Symbols.None)
        {
            return Operations.GetProposals(hash, quotes);
        }

        /// <summary>
        /// Get proposals count
        /// </summary>
        /// <remarks>
        /// Returns the total number of proposal operations.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("proposals/count")]
        public Task<int> GetProposalsCount()
        {
            return Task.FromResult(State.GetState().ProposalOpsCount);
        }
        #endregion

        #region activations
        /// <summary>
        /// Get activations
        /// </summary>
        /// <remarks>
        /// Returns a list of activation operations.
        /// </remarks>
        /// <param name="level">Filters activations by level.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts activations by specified field. Supported fields: `id` (default), `level`, `balance`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("activations")]
        public async Task<ActionResult<IEnumerable<ActivationOperation>>> GetActivations(
            Int32Parameter level,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quotes = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "balance"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetActivations(level, sort, offset, limit, quotes));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetActivations(level, sort, offset, limit, select.Values[0], quotes));
                else
                    return Ok(await Operations.GetActivations(level, sort, offset, limit, select.Values, quotes));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetActivations(level, sort, offset, limit, select.Fields[0], quotes));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetActivations(level, sort, offset, limit, select.Fields, quotes)
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
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("activations/{hash}")]
        public Task<IEnumerable<ActivationOperation>> GetActivationByHash([OpHash] string hash, Symbols quotes = Symbols.None)
        {
            return Operations.GetActivations(hash, quotes);
        }

        /// <summary>
        /// Get activations count
        /// </summary>
        /// <remarks>
        /// Returns the total number of activation operations.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("activations/count")]
        public Task<int> GetActivationsCount()
        {
            return Task.FromResult(State.GetState().ActivationOpsCount);
        }
        #endregion

        #region double baking
        /// <summary>
        /// Get double baking
        /// </summary>
        /// <remarks>
        /// Returns a list of double baking operations.
        /// </remarks>
        /// <param name="level">Filters double baking operations by level.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts double baking operations by specified field. Supported fields: `id` (default), `level`, `accusedLevel`, `accuserRewards`, `offenderLostDeposits`, `offenderLostRewards`, `offenderLostFees`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("double_baking")]
        public async Task<ActionResult<IEnumerable<DoubleBakingOperation>>> GetDoubleBaking(
            Int32Parameter level,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quotes = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "accusedLevel", "accuserRewards", "offenderLostDeposits", "offenderLostRewards", "offenderLostFees"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetDoubleBakings(level, sort, offset, limit, quotes));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetDoubleBakings(level, sort, offset, limit, select.Values[0], quotes));
                else
                    return Ok(await Operations.GetDoubleBakings(level, sort, offset, limit, select.Values, quotes));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetDoubleBakings(level, sort, offset, limit, select.Fields[0], quotes));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetDoubleBakings(level, sort, offset, limit, select.Fields, quotes)
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
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("double_baking/{hash}")]
        public Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakingByHash([OpHash] string hash, Symbols quotes = Symbols.None)
        {
            return Operations.GetDoubleBakings(hash, quotes);
        }

        /// <summary>
        /// Get double baking count
        /// </summary>
        /// <remarks>
        /// Returns the total number of double baking operations.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("double_baking/count")]
        public Task<int> GetDoubleBakingCount()
        {
            return Task.FromResult(State.GetState().DoubleBakingOpsCount);
        }
        #endregion

        #region double endorsing
        /// <summary>
        /// Get double endorsing
        /// </summary>
        /// <remarks>
        /// Returns a list of double endorsing operations.
        /// </remarks>
        /// <param name="level">Filters double endorsing operations by level.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts double endorsing operations by specified field. Supported fields: `id` (default), `level`, `accusedLevel`, `accuserRewards`, `offenderLostDeposits`, `offenderLostRewards`, `offenderLostFees`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("double_endorsing")]
        public async Task<ActionResult<IEnumerable<DoubleEndorsingOperation>>> GetDoubleEndorsing(
            Int32Parameter level,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quotes = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "accusedLevel", "accuserRewards", "offenderLostDeposits", "offenderLostRewards", "offenderLostFees"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetDoubleEndorsings(level, sort, offset, limit, quotes));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetDoubleEndorsings(level, sort, offset, limit, select.Values[0], quotes));
                else
                    return Ok(await Operations.GetDoubleEndorsings(level, sort, offset, limit, select.Values, quotes));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetDoubleEndorsings(level, sort, offset, limit, select.Fields[0], quotes));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetDoubleEndorsings(level, sort, offset, limit, select.Fields, quotes)
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
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("double_endorsing/{hash}")]
        public Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsingByHash([OpHash] string hash, Symbols quotes = Symbols.None)
        {
            return Operations.GetDoubleEndorsings(hash, quotes);
        }

        /// <summary>
        /// Get double endorsing count
        /// </summary>
        /// <remarks>
        /// Returns the total number of double endorsing operations.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("double_endorsing/count")]
        public Task<int> GetDoubleEndorsingCount()
        {
            return Task.FromResult(State.GetState().DoubleEndorsingOpsCount);
        }
        #endregion

        #region nonce revelations
        /// <summary>
        /// Get nonce revelations
        /// </summary>
        /// <remarks>
        /// Returns a list of seed nonce revelation operations.
        /// </remarks>
        /// <param name="level">Filters nonce revelation operations by level.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts nonce revelation operations by specified field. Supported fields: `id` (default), `level`, `revealedLevel`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("nonce_revelations")]
        public async Task<ActionResult<IEnumerable<NonceRevelationOperation>>> GetNonceRevelations(
            Int32Parameter level,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quotes = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "revealedLevel"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetNonceRevelations(level, sort, offset, limit, quotes));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetNonceRevelations(level, sort, offset, limit, select.Values[0], quotes));
                else
                    return Ok(await Operations.GetNonceRevelations(level, sort, offset, limit, select.Values, quotes));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetNonceRevelations(level, sort, offset, limit, select.Fields[0], quotes));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetNonceRevelations(level, sort, offset, limit, select.Fields, quotes)
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
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("nonce_revelations/{hash}")]
        public Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelationByHash([OpHash] string hash, Symbols quotes = Symbols.None)
        {
            return Operations.GetNonceRevelations(hash, quotes);
        }

        /// <summary>
        /// Get nonce revelations count
        /// </summary>
        /// <remarks>
        /// Returns the total number of seed nonce revelation operations.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("nonce_revelations/count")]
        public Task<int> GetNonceRevelationsCount()
        {
            return Task.FromResult(State.GetState().NonceRevelationOpsCount);
        }
        #endregion

        #region delegations
        /// <summary>
        /// Get delegations
        /// </summary>
        /// <remarks>
        /// Returns a list of delegation operations.
        /// </remarks>
        /// <param name="initiator">Filters delegations by initiator. Allowed fields for `.eqx` mode: `prevDelegate`, `newDelegate`.</param>
        /// <param name="sender">Filters delegations by sender. Allowed fields for `.eqx` mode: `prevDelegate`, `newDelegate`.</param>
        /// <param name="prevDelegate">Filters delegations by prev delegate. Allowed fields for `.eqx` mode: `initiator`, `sender`, `newDelegate`.</param>
        /// <param name="newDelegate">Filters delegations by new delegate. Allowed fields for `.eqx` mode: `initiator`, `sender`, `prevDelegate`.</param>
        /// <param name="level">Filters delegations by level.</param>
        /// <param name="status">Filters delegations by operation status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts delegations by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `bakerFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("delegations")]
        public async Task<ActionResult<IEnumerable<DelegationOperation>>> GetDelegations(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            Int32Parameter level,
            OperationStatusParameter status,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quotes = Symbols.None)
        {
            #region validate
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

                if (prevDelegate.Eq == -1 || prevDelegate.In?.Count == 0 || prevDelegate.Null == true)
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
                return Ok(await Operations.GetDelegations(initiator, sender, prevDelegate, newDelegate, level, status, sort, offset, limit, quotes));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetDelegations(initiator, sender, prevDelegate, newDelegate, level, status, sort, offset, limit, select.Values[0], quotes));
                else
                    return Ok(await Operations.GetDelegations(initiator, sender, prevDelegate, newDelegate, level, status, sort, offset, limit, select.Values, quotes));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetDelegations(initiator, sender, prevDelegate, newDelegate, level, status, sort, offset, limit, select.Fields[0], quotes));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetDelegations(initiator, sender, prevDelegate, newDelegate, level, status, sort, offset, limit, select.Fields, quotes)
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
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("delegations/{hash}")]
        public Task<IEnumerable<DelegationOperation>> GetDelegationByHash([OpHash] string hash, Symbols quotes = Symbols.None)
        {
            return Operations.GetDelegations(hash, quotes);
        }

        /// <summary>
        /// Get delegations count
        /// </summary>
        /// <remarks>
        /// Returns the total number of delegation operations.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("delegations/count")]
        public Task<int> GetDelegationsCount()
        {
            return Task.FromResult(State.GetState().DelegationOpsCount);
        }
        #endregion

        #region originations
        /// <summary>
        /// Get originations
        /// </summary>
        /// <remarks>
        /// Returns a list of origination operations.
        /// </remarks>
        /// <param name="initiator">Filters origination operations by initiator. Allowed fields for `.eqx` mode: `contractManager`, `contractDelegate`.</param>
        /// <param name="sender">Filters origination operations by sender. Allowed fields for `.eqx` mode: `contractManager`, `contractDelegate`.</param>
        /// <param name="contractManager">Filters origination operations by manager. Allowed fields for `.eqx` mode: `initiator`, `sender`, `contractDelegate`.</param>
        /// <param name="contractDelegate">Filters origination operations by delegate. Allowed fields for `.eqx` mode: `initiator`, `sender`, `contractManager`.</param>
        /// <param name="originatedContract">Filters origination operations by originated contract. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters origination operations by level.</param>
        /// <param name="status">Filters origination operations by operation status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts originations by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `storageUsed`, `bakerFee`, `storageFee`, `allocationFee`, `contractBalance`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("originations")]
        public async Task<ActionResult<IEnumerable<OriginationOperation>>> GetOriginations(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter contractManager,
            AccountParameter contractDelegate,
            AccountParameter originatedContract,
            Int32Parameter level,
            OperationStatusParameter status,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quotes = Symbols.None)
        {
            #region validates
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
                return Ok(await Operations.GetOriginations(initiator, sender, contractManager, contractDelegate, originatedContract, level, status, sort, offset, limit, quotes));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetOriginations(initiator, sender, contractManager, contractDelegate, originatedContract, level, status, sort, offset, limit, select.Values[0], quotes));
                else
                    return Ok(await Operations.GetOriginations(initiator, sender, contractManager, contractDelegate, originatedContract, level, status, sort, offset, limit, select.Values, quotes));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetOriginations(initiator, sender, contractManager, contractDelegate, originatedContract, level, status, sort, offset, limit, select.Fields[0], quotes));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetOriginations(initiator, sender, contractManager, contractDelegate, originatedContract, level, status, sort, offset, limit, select.Fields, quotes)
                    });
                }
            }
        }

        /// <summary>
        /// Get origination by hash
        /// </summary>
        /// <remarks>
        /// Returns a origination operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("originations/{hash}")]
        public Task<IEnumerable<OriginationOperation>> GetOriginationByHash([OpHash] string hash, Symbols quotes = Symbols.None)
        {
            return Operations.GetOriginations(hash, quotes);
        }

        /// <summary>
        /// Get originations count
        /// </summary>
        /// <remarks>
        /// Returns the total number of origination operations.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("originations/count")]
        public Task<int> GetOriginationsCount()
        {
            return Task.FromResult(State.GetState().OriginationOpsCount);
        }
        #endregion

        #region transactions
        /// <summary>
        /// Get transactions
        /// </summary>
        /// <remarks>
        /// Returns a list of transaction operations.
        /// </remarks>
        /// <param name="initiator">Filters transactions by initiator. Allowed fields for `.eqx` mode: `target`.</param>
        /// <param name="sender">Filters transactions by sender. Allowed fields for `.eqx` mode: `target`.</param>
        /// <param name="target">Filters transactions by target. Allowed fields for `.eqx` mode: `sender`, `initiator`.</param>
        /// <param name="amount">Filters transactions by amount (microtez).</param>
        /// <param name="level">Filters transactions by level.</param>
        /// <param name="parameters">Filters transactions by parameters value.  Allowed fields for `.eqx` mode: not supported.</param>
        /// <param name="status">Filters transactions by operation status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts transactions by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `storageUsed`, `bakerFee`, `storageFee`, `allocationFee`, `amount`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("transactions")]
        public async Task<ActionResult<IEnumerable<TransactionOperation>>> GetTransactions(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            Int64Parameter amount,
            Int32Parameter level,
            StringParameter parameters,
            OperationStatusParameter status,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quotes = Symbols.None) 
        {
            #region validate
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

            if (parameters != null)
            {
                if (parameters.Eqx != null)
                    return new BadRequest($"{nameof(parameters)}.eqx", "This parameter doesn't support .eqx mode.");

                if (parameters.Nex != null)
                    return new BadRequest($"{nameof(parameters)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "storageUsed", "bakerFee", "storageFee", "allocationFee", "amount"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetTransactions(initiator, sender, target, amount, level, parameters, status, sort, offset, limit, quotes));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetTransactions(initiator, sender, target, amount, level, parameters, status, sort, offset, limit, select.Values[0], quotes));
                else
                    return Ok(await Operations.GetTransactions(initiator, sender, target, amount, level, parameters, status, sort, offset, limit, select.Values, quotes));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetTransactions(initiator, sender, target, amount, level, parameters, status, sort, offset, limit, select.Fields[0], quotes));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetTransactions(initiator, sender, target, amount, level, parameters, status, sort, offset, limit, select.Fields, quotes)
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
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("transactions/{hash}")]
        public Task<IEnumerable<TransactionOperation>> GetTransactionByHash([OpHash] string hash, Symbols quotes = Symbols.None)
        {
            return Operations.GetTransactions(hash, quotes);
        }

        /// <summary>
        /// Get transaction by hash and counter
        /// </summary>
        /// <remarks>
        /// Returns transaction operations with specified hash and counter.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="counter">Operation counter</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("transactions/{hash}/{counter}")]
        public Task<IEnumerable<TransactionOperation>> GetTransactionByHashCounter([OpHash] string hash, [Min(0)] int counter, Symbols quotes = Symbols.None)
        {
            return Operations.GetTransactions(hash, counter, quotes);
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
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("transactions/{hash}/{counter}/{nonce}")]
        public Task<IEnumerable<TransactionOperation>> GetTransactionByHashCounterNonce([OpHash] string hash, [Min(0)] int counter, [Min(0)] int nonce, Symbols quotes = Symbols.None)
        {
            return Operations.GetTransactions(hash, counter, nonce, quotes);
        }

        /// <summary>
        /// Get transactions count
        /// </summary>
        /// <remarks>
        /// Returns the total number of transaction operations.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("transactions/count")]
        public Task<int> GetTransactionsCount()
        {
            return Task.FromResult(State.GetState().TransactionOpsCount);
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
        /// <param name="status">Filters reveal operations by operation status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts reveals by specified field. Supported fields: `id` (default), `level`, `gasUsed`, `bakerFee`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("reveals")]
        public async Task<ActionResult<IEnumerable<RevealOperation>>> GetReveals(
            AccountParameter sender,
            Int32Parameter level,
            OperationStatusParameter status,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quotes = Symbols.None)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.nex", "This parameter doesn't support .nex mode.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<OriginationOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "gasUsed", "bakerFee"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetReveals(sender, level, status, sort, offset, limit, quotes));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetReveals(sender, level, status, sort, offset, limit, select.Values[0], quotes));
                else
                    return Ok(await Operations.GetReveals(sender, level, status, sort, offset, limit, select.Values, quotes));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetReveals(sender, level, status, sort, offset, limit, select.Fields[0], quotes));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetReveals(sender, level, status, sort, offset, limit, select.Fields, quotes)
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
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("reveals/{hash}")]
        public Task<IEnumerable<RevealOperation>> GetRevealByHash([OpHash] string hash, Symbols quotes = Symbols.None)
        {
            return Operations.GetReveals(hash, quotes);
        }

        /// <summary>
        /// Get reveals count
        /// </summary>
        /// <remarks>
        /// Returns the total number of reveal operations.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("reveals/count")]
        public Task<int> GetRevealsCount()
        {
            return Task.FromResult(State.GetState().RevealOpsCount);
        }
        #endregion

        #region migrations
        /// <summary>
        /// Get migrations
        /// </summary>
        /// <remarks>
        /// Returns a list of migration operations (synthetic type).
        /// </remarks>
        /// <param name="level">Filters migration operations by level.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts migrations by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("migrations")]
        public async Task<ActionResult<IEnumerable<MigrationOperation>>> GetMigrations(
            Int32Parameter level,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quotes = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetMigrations(level, sort, offset, limit, quotes));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetMigrations(level, sort, offset, limit, select.Values[0], quotes));
                else
                    return Ok(await Operations.GetMigrations(level, sort, offset, limit, select.Values, quotes));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetMigrations(level, sort, offset, limit, select.Fields[0], quotes));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetMigrations(level, sort, offset, limit, select.Fields, quotes)
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
        /// <returns></returns>
        [HttpGet("migrations/count")]
        public Task<int> GetMigrationsCount()
        {
            return Task.FromResult(State.GetState().MigrationOpsCount);
        }
        #endregion

        #region revelation penalties
        /// <summary>
        /// Get revelation penalties
        /// </summary>
        /// <remarks>
        /// Returns a list of revelation penalty operations (synthetic type).
        /// </remarks>
        /// <param name="level">Filters revelation penalty operations by level.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts revelation penalty operations by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("revelation_penalties")]
        public async Task<ActionResult<IEnumerable<RevelationPenaltyOperation>>> GetRevelationPenalties(
            Int32Parameter level,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quotes = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetRevelationPenalties(level, sort, offset, limit, quotes));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetRevelationPenalties(level, sort, offset, limit, select.Values[0], quotes));
                else
                    return Ok(await Operations.GetRevelationPenalties(level, sort, offset, limit, select.Values, quotes));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetRevelationPenalties(level, sort, offset, limit, select.Fields[0], quotes));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetRevelationPenalties(level, sort, offset, limit, select.Fields, quotes)
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
        /// <returns></returns>
        [HttpGet("revelation_penalties/count")]
        public Task<int> GetRevelationPenaltiesCount()
        {
            return Task.FromResult(State.GetState().RevelationPenaltyOpsCount);
        }
        #endregion

        #region baking
        /// <summary>
        /// Get baking
        /// </summary>
        /// <remarks>
        /// Returns a list of baking operations (synthetic type).
        /// </remarks>
        /// <param name="level">Filters baking operations by level.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts baking operations by specified field. Supported fields: `id` (default), `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quotes">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("baking")]
        public async Task<ActionResult<IEnumerable<BakingOperation>>> GetBaking(
            Int32Parameter level,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quotes = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Operations.GetBakings(level, sort, offset, limit, quotes));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Operations.GetBakings(level, sort, offset, limit, select.Values[0], quotes));
                else
                    return Ok(await Operations.GetBakings(level, sort, offset, limit, select.Values, quotes));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Operations.GetBakings(level, sort, offset, limit, select.Fields[0], quotes));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Operations.GetBakings(level, sort, offset, limit, select.Fields, quotes)
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
        /// <returns></returns>
        [HttpGet("baking/count")]
        public Task<int> GetBakingCount()
        {
            return Task.FromResult(State.GetState().BlocksCount - 2);
        }
        #endregion
    }
}
