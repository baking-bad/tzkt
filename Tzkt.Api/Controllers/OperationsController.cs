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
    [Route("v1/operations")]
    public class OperationsController : ControllerBase
    {
        private readonly OperationRepository Operations;

        public OperationsController(OperationRepository operations)
        {
            Operations = operations;
        }

        #region operations
        /// <summary>
        /// Get operations by hash
        /// </summary>
        /// <remarks>
        /// Returns a list of operations with the specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("{hash}")]
        public Task<IEnumerable<Operation>> GetByHash([OpHash] string hash)
        {
            return Operations.Get(hash);
        }

        /// <summary>
        /// Get operations by hash and counter
        /// </summary>
        /// <remarks>
        /// Returns a list of operations with the specified hash and counter.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="counter">Operation counter</param>
        /// <returns></returns>
        [HttpGet("{hash}/{counter}")]
        public Task<IEnumerable<Operation>> GetByHashCounter([OpHash] string hash, [Min(0)] int counter)
        {
            return Operations.Get(hash, counter);
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
        /// <returns></returns>
        [HttpGet("{hash}/{counter}/{nonce}")]
        public Task<IEnumerable<Operation>> GetByHashCounterNonce([OpHash] string hash, [Min(0)] int counter, [Min(0)] int nonce)
        {
            return Operations.Get(hash, counter, nonce);
        }
        #endregion

        #region endorsements
        /// <summary>
        /// Get endorsements
        /// </summary>
        /// <remarks>
        /// Returns a list of endorsement operations.
        /// </remarks>
        /// <param name="sort">Sorts endorsements by specified field. Supported fields: `id`, `level`, `timestamp`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet("endorsements")]
        public async Task<ActionResult<IEnumerable<EndorsementOperation>>> GetEndorsements(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "timestamp"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Operations.GetEndorsements(sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Operations.GetEndorsements(sort, offset, limit, fields[0]));

            return Ok(await Operations.GetEndorsements(sort, offset, limit, fields));
        }

        /// <summary>
        /// Get endorsement by hash
        /// </summary>
        /// <remarks>
        /// Returns an endorsement operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("endorsements/{hash}")]
        public Task<IEnumerable<EndorsementOperation>> GetEndorsementByHash([OpHash] string hash)
        {
            return Operations.GetEndorsements(hash);
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
            return Operations.GetEndorsementsCount();
        }
        #endregion

        #region proposals
        /// <summary>
        /// Get proposals
        /// </summary>
        /// <remarks>
        /// Returns a list of proposal operations.
        /// </remarks>
        /// <param name="sort">Sorts proposal operations by specified field. Supported fields: `id`, `level`, `timestamp`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet("proposals")]
        public async Task<ActionResult<IEnumerable<ProposalOperation>>> GetProposals(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "timestamp"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Operations.GetProposals(sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Operations.GetProposals(sort, offset, limit, fields[0]));

            return Ok(await Operations.GetProposals(sort, offset, limit, fields));
        }

        /// <summary>
        /// Get proposal by hash
        /// </summary>
        /// <remarks>
        /// Returns a proposal operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("proposals/{hash}")]
        public Task<IEnumerable<ProposalOperation>> GetProposalByHash([OpHash] string hash)
        {
            return Operations.GetProposals(hash);
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
            return Operations.GetProposalsCount();
        }
        #endregion

        #region ballots
        /// <summary>
        /// Get ballots
        /// </summary>
        /// <remarks>
        /// Returns a list of ballot operations.
        /// </remarks>
        /// <param name="sort">Sorts ballots by specified field. Supported fields: `id`, `level`, `timestamp`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet("ballots")]
        public async Task<ActionResult<IEnumerable<BallotOperation>>> GetBallots(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "timestamp"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Operations.GetBallots(sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Operations.GetBallots(sort, offset, limit, fields[0]));

            return Ok(await Operations.GetBallots(sort, offset, limit, fields));
        }

        /// <summary>
        /// Get ballot by hash
        /// </summary>
        /// <remarks>
        /// Returns a ballot operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("ballots/{hash}")]
        public Task<IEnumerable<BallotOperation>> GetBallotByHash([OpHash] string hash)
        {
            return Operations.GetBallots(hash);
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
            return Operations.GetBallotsCount();
        }
        #endregion

        #region activations
        /// <summary>
        /// Get activations
        /// </summary>
        /// <remarks>
        /// Returns a list of activation operations.
        /// </remarks>
        /// <param name="sort">Sorts activations by specified field. Supported fields: `id`, `level`, `timestamp`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet("activations")]
        public async Task<ActionResult<IEnumerable<ActivationOperation>>> GetActivations(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "timestamp"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Operations.GetActivations(sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Operations.GetActivations(sort, offset, limit, fields[0]));

            return Ok(await Operations.GetActivations(sort, offset, limit, fields));
        }

        /// <summary>
        /// Get activation by hash
        /// </summary>
        /// <remarks>
        /// Returns an activation operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("activations/{hash}")]
        public Task<IEnumerable<ActivationOperation>> GetActivationByHash([OpHash] string hash)
        {
            return Operations.GetActivations(hash);
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
            return Operations.GetActivationsCount();
        }
        #endregion

        #region double baking
        /// <summary>
        /// Get double baking
        /// </summary>
        /// <remarks>
        /// Returns a list of double baking operations.
        /// </remarks>
        /// <param name="sort">Sorts double baking operations by specified field. Supported fields: `id`, `level`, `timestamp`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet("double_baking")]
        public async Task<ActionResult<IEnumerable<DoubleBakingOperation>>> GetDoubleBaking(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "timestamp"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Operations.GetDoubleBakings(sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Operations.GetDoubleBakings(sort, offset, limit, fields[0]));

            return Ok(await Operations.GetDoubleBakings(sort, offset, limit, fields));
        }

        /// <summary>
        /// Get double baking by hash
        /// </summary>
        /// <remarks>
        /// Returns a double baking operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("double_baking/{hash}")]
        public Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakingByHash([OpHash] string hash)
        {
            return Operations.GetDoubleBakings(hash);
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
            return Operations.GetDoubleBakingsCount();
        }
        #endregion

        #region double endorsing
        /// <summary>
        /// Get double endorsing
        /// </summary>
        /// <remarks>
        /// Returns a list of double endorsing operations.
        /// </remarks>
        /// <param name="sort">Sorts double endorsing operations by specified field. Supported fields: `id`, `level`, `timestamp`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet("double_endorsing")]
        public async Task<ActionResult<IEnumerable<DoubleEndorsingOperation>>> GetDoubleEndorsing(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "timestamp"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Operations.GetDoubleEndorsings(sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Operations.GetDoubleEndorsings(sort, offset, limit, fields[0]));

            return Ok(await Operations.GetDoubleEndorsings(sort, offset, limit, fields));
        }

        /// <summary>
        /// Get double endorsing by hash
        /// </summary>
        /// <remarks>
        /// Returns a double endorsing operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("double_endorsing/{hash}")]
        public Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsingByHash([OpHash] string hash)
        {
            return Operations.GetDoubleEndorsings(hash);
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
            return Operations.GetDoubleEndorsingsCount();
        }
        #endregion

        #region nonce revelations
        /// <summary>
        /// Get nonce revelations
        /// </summary>
        /// <remarks>
        /// Returns a list of seed nonce revelation operations.
        /// </remarks>
        /// <param name="sort">Sorts nonce revelation operations by specified field. Supported fields: `id`, `level`, `timestamp`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet("nonce_revelations")]
        public async Task<ActionResult<IEnumerable<NonceRevelationOperation>>> GetNonceRevelations(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "timestamp"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Operations.GetNonceRevelations(sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Operations.GetNonceRevelations(sort, offset, limit, fields[0]));

            return Ok(await Operations.GetNonceRevelations(sort, offset, limit, fields));
        }

        /// <summary>
        /// Get nonce revelation by hash
        /// </summary>
        /// <remarks>
        /// Returns a seed nonce revelation operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("nonce_revelations/{hash}")]
        public Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelationByHash([OpHash] string hash)
        {
            return Operations.GetNonceRevelations(hash);
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
            return Operations.GetNonceRevelationsCount();
        }
        #endregion

        #region delegations
        /// <summary>
        /// Get delegations
        /// </summary>
        /// <remarks>
        /// Returns a list of delegation operations.
        /// </remarks>
        /// <param name="sender">Filters delegations by sender. Allowed fields for `.eqx` mode: `prevDelegate`, `newDelegate`.</param>
        /// <param name="prevDelegate">Filters delegations by prev delegate. Allowed fields for `.eqx` mode: `sender`, `newDelegate`.</param>
        /// <param name="newDelegate">Filters delegations by new delegate. Allowed fields for `.eqx` mode: `sender`, `prevDelegate`.</param>
        /// <param name="status">Filters delegations by operation status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="sort">Sorts delegations by specified field. Supported fields: `id`, `level`, `timestamp`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next version.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next version.</param>
        /// <returns></returns>
        [HttpGet("delegations")]
        public async Task<ActionResult<IEnumerable<DelegationOperation>>> GetDelegations(
            AccountParameter sender,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 1000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sender != null)
            {
                if (sender.Eqx != null && sender.Eqx != "prevDelegate" && sender.Eqx != "newDelegate")
                    return new BadRequest($"{nameof(sender)}.eqx", "The 'sender' field can be compared with the 'prevDelegate' or 'newDelegate' field only.");

                if (sender.Nex != null && sender.Nex != "prevDelegate" && sender.Nex != "newDelegate")
                    return new BadRequest($"{nameof(sender)}.nex", "The 'sender' field can be compared with the 'prevDelegate' or 'newDelegate' field only.");

                if (sender.Eq == -1 || sender.In?.Count == 0 || sender.Null == true)
                    return Ok(Enumerable.Empty<TransactionOperation>());
            }

            if (prevDelegate != null)
            {
                if (prevDelegate.Eqx != null && prevDelegate.Eqx != "sender" && prevDelegate.Eqx != "newDelegate")
                    return new BadRequest($"{nameof(prevDelegate)}.eqx", "The 'prevDelegate' field can be compared with the 'sender' or 'newDelegate' field only.");

                if (prevDelegate.Nex != null && prevDelegate.Nex != "sender" && prevDelegate.Nex != "newDelegate")
                    return new BadRequest($"{nameof(prevDelegate)}.nex", "The 'prevDelegate' field can be compared with the 'sender' or 'newDelegate' field only.");

                if (prevDelegate.Eq == -1 || prevDelegate.In?.Count == 0 || prevDelegate.Null == true)
                    return Ok(Enumerable.Empty<TransactionOperation>());
            }

            if (newDelegate != null)
            {
                if (newDelegate.Eqx != null && newDelegate.Eqx != "sender" && newDelegate.Eqx != "prevDelegate")
                    return new BadRequest($"{nameof(newDelegate)}.eqx", "The 'newDelegate' field can be compared with the 'sender' or 'prevDelegate' fields only.");

                if (newDelegate.Nex != null && newDelegate.Nex != "sender" && newDelegate.Nex != "prevDelegate")
                    return new BadRequest($"{nameof(newDelegate)}.nex", "The 'newDelegate' field can be compared with the 'sender' or 'prevDelegate' fields only.");

                if (newDelegate.Eq == -1 || newDelegate.In?.Count == 0)
                    return Ok(Enumerable.Empty<TransactionOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "timestamp"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Operations.GetDelegations(sender, prevDelegate, newDelegate, status, sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Operations.GetDelegations(sender, prevDelegate, newDelegate, status, sort, offset, limit, fields[0]));

            return Ok(await Operations.GetDelegations(sender, prevDelegate, newDelegate, status, sort, offset, limit, fields));
        }

        /// <summary>
        /// Get delegation by hash
        /// </summary>
        /// <remarks>
        /// Returns a delegation operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("delegations/{hash}")]
        public Task<IEnumerable<DelegationOperation>> GetDelegationByHash([OpHash] string hash)
        {
            return Operations.GetDelegations(hash);
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
            return Operations.GetDelegationsCount();
        }
        #endregion

        #region originations
        /// <summary>
        /// Get originations
        /// </summary>
        /// <remarks>
        /// Returns a list of origination operations.
        /// </remarks>
        /// <param name="sort">Sorts originations by specified field. Supported fields: `id`, `level`, `timestamp`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet("originations")]
        public async Task<ActionResult<IEnumerable<OriginationOperation>>> GetOriginations(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "timestamp"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Operations.GetOriginations(sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Operations.GetOriginations(sort, offset, limit, fields[0]));

            return Ok(await Operations.GetOriginations(sort, offset, limit, fields));
        }

        /// <summary>
        /// Get origination by hash
        /// </summary>
        /// <remarks>
        /// Returns a origination operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("originations/{hash}")]
        public Task<IEnumerable<OriginationOperation>> GetOriginationByHash([OpHash] string hash)
        {
            return Operations.GetOriginations(hash);
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
            return Operations.GetOriginationsCount();
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
        /// <param name="parameters">Filters transactions by parameters value.  Allowed fields for `.eqx` mode: not supported.</param>
        /// <param name="sort">Sorts transactions by specified field. Supported fields: `id`, `level`, `timestamp`, `amount`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next version.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next version.</param>
        /// <returns></returns>
        [HttpGet("transactions")]
        public async Task<ActionResult<IEnumerable<TransactionOperation>>> GetTransactions(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            StringParameter parameters,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 1000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100) 
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

            if (sort != null && !sort.Validate("id", "level", "timestamp", "amount"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Operations.GetTransactions(initiator, sender, target, parameters, sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Operations.GetTransactions(initiator, sender, target, parameters, sort, offset, limit, fields[0]));

            return Ok(await Operations.GetTransactions(initiator, sender, target, parameters, sort, offset, limit, fields));
        }

        /// <summary>
        /// Get transaction by hash
        /// </summary>
        /// <remarks>
        /// Returns transaction operations with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("transactions/{hash}")]
        public Task<IEnumerable<TransactionOperation>> GetTransactionByHash([OpHash] string hash)
        {
            return Operations.GetTransactions(hash);
        }

        /// <summary>
        /// Get transaction by hash and counter
        /// </summary>
        /// <remarks>
        /// Returns transaction operations with specified hash and counter.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <param name="counter">Operation counter</param>
        /// <returns></returns>
        [HttpGet("transactions/{hash}/{counter}")]
        public Task<IEnumerable<TransactionOperation>> GetTransactionByHashCounter([OpHash] string hash, [Min(0)] int counter)
        {
            return Operations.GetTransactions(hash, counter);
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
        /// <returns></returns>
        [HttpGet("transactions/{hash}/{counter}/{nonce}")]
        public Task<IEnumerable<TransactionOperation>> GetTransactionByHashCounterNonce([OpHash] string hash, [Min(0)] int counter, [Min(0)] int nonce)
        {
            return Operations.GetTransactions(hash, counter, nonce);
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
            return Operations.GetTransactionsCount();
        }
        #endregion

        #region reveals
        /// <summary>
        /// Get reveals
        /// </summary>
        /// <remarks>
        /// Returns a list of reveal operations.
        /// </remarks>
        /// <param name="sort">Sorts reveals by specified field. Supported fields: `id`, `level`, `timestamp`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet("reveals")]
        public async Task<ActionResult<IEnumerable<RevealOperation>>> GetReveals(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "timestamp"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Operations.GetReveals(sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Operations.GetReveals(sort, offset, limit, fields[0]));

            return Ok(await Operations.GetReveals(sort, offset, limit, fields));
        }

        /// <summary>
        /// Get reveal by hash
        /// </summary>
        /// <remarks>
        /// Returns reveal operation with specified hash.
        /// </remarks>
        /// <param name="hash">Operation hash</param>
        /// <returns></returns>
        [HttpGet("reveals/{hash}")]
        public Task<IEnumerable<RevealOperation>> GetRevealByHash([OpHash] string hash)
        {
            return Operations.GetReveals(hash);
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
            return Operations.GetRevealsCount();
        }
        #endregion

        #region migrations
        /// <summary>
        /// Get migrations
        /// </summary>
        /// <remarks>
        /// Returns a list of migration operations (synthetic type).
        /// </remarks>
        /// <param name="sort">Sorts migrations by specified field. Supported fields: `id`, `level`, `timestamp`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet("migrations")]
        public async Task<ActionResult<IEnumerable<MigrationOperation>>> GetMigrations(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "timestamp"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Operations.GetMigrations(sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Operations.GetMigrations(sort, offset, limit, fields[0]));

            return Ok(await Operations.GetMigrations(sort, offset, limit, fields));
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
            return Operations.GetMigrationsCount();
        }
        #endregion

        #region revelation penalties
        /// <summary>
        /// Get revelation penalties
        /// </summary>
        /// <remarks>
        /// Returns a list of revelation penalty operations (synthetic type).
        /// </remarks>
        /// <param name="sort">Sorts revelation penalty operations by specified field. Supported fields: `id`, `level`, `timestamp`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet("revelation_penalties")]
        public async Task<ActionResult<IEnumerable<RevelationPenaltyOperation>>> GetRevelationPenalties(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "timestamp"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Operations.GetRevelationPenalties(sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Operations.GetRevelationPenalties(sort, offset, limit, fields[0]));

            return Ok(await Operations.GetRevelationPenalties(sort, offset, limit, fields));
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
            return Operations.GetRevelationPenaltiesCount();
        }
        #endregion

        #region baking
        /// <summary>
        /// Get baking
        /// </summary>
        /// <remarks>
        /// Returns a list of baking operations (synthetic type).
        /// </remarks>
        /// <param name="sort">Sorts baking operations by specified field. Supported fields: `id`, `level`, `timestamp`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet("baking")]
        public async Task<ActionResult<IEnumerable<BakingOperation>>> GetBaking(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "timestamp"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Operations.GetBakings(sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Operations.GetBakings(sort, offset, limit, fields[0]));

            return Ok(await Operations.GetBakings(sort, offset, limit, fields));
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
            return Operations.GetBakingsCount();
        }
        #endregion
    }
}
