using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/commitments")]
    public class CommitmentsController : ControllerBase
    {
        private readonly CommitmentRepository Commitments;
        private readonly StateCache State;

        public CommitmentsController(CommitmentRepository commitments, StateCache state)
        {
            Commitments = commitments;
            State = state;
        }

        /// <summary>
        /// Get commitment by blinded address
        /// </summary>
        /// <remarks>
        /// Returns a commitment with the specified blinded address.
        /// </remarks>
        /// <param name="address">Blinded address (starting with btz)</param>
        /// <returns></returns>
        [HttpGet("{address}")]
        public Task<Commitment> Get([Required][BlindedAddress] string address)
        {
            return Commitments.Get(address);
        }

        /// <summary>
        /// Get commitments
        /// </summary>
        /// <remarks>
        /// Returns a list of commitments.
        /// </remarks>
        /// <param name="activated">Filters commitments by activation status</param>
        /// <param name="activationLevel">Filters commitments by activation level</param>
        /// <param name="balance">Filters commitments by activated balance</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `id` (default), `balance`, `activationLevel`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Commitment>>> Get(
            bool? activated,
            Int32NullParameter activationLevel,
            Int64Parameter balance,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("activationLevel", "balance"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Commitments.Get(activated, activationLevel, balance, sort, offset, limit));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Commitments.Get(activated, activationLevel, balance, sort, offset, limit, select.Values[0]));
                else
                    return Ok(await Commitments.Get(activated, activationLevel, balance, sort, offset, limit, select.Values));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Commitments.Get(activated, activationLevel, balance, sort, offset, limit, select.Fields[0]));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Commitments.Get(activated, activationLevel, balance, sort, offset, limit, select.Fields)
                    });
                }
            }
        }

        /// <summary>
        /// Get commitments count
        /// </summary>
        /// <remarks>
        /// Returns a number of commitments.
        /// </remarks>
        /// <param name="activated">Filters commitments by activation status</param>
        /// <param name="balance">Filters commitments by activated balance</param>
        /// <returns></returns>
        [HttpGet("count")]
        public Task<int> GetCount(bool? activated, Int64Parameter balance)
        {
            #region optimize
            if (balance == null)
            {
                var state = State.Current;

                if (activated == null)
                    return Task.FromResult(state.CommitmentsCount);
                else if (activated == true)
                    return Task.FromResult(state.ActivationOpsCount);
                else
                    return Task.FromResult(state.CommitmentsCount - state.ActivationOpsCount);
            }
            #endregion

            return Commitments.GetCount(activated, balance);
        }
    }
}
