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
    [Route("v1/delegates")]
    public class DelegatesController : ControllerBase
    {
        private readonly AccountRepository Accounts;
        public DelegatesController(AccountRepository accounts)
        {
            Accounts = accounts;
        }

        /// <summary>
        /// Get delegates
        /// </summary>
        /// <remarks>
        /// Returns a list of delegate accounts.
        /// </remarks>
        /// <param name="active">Delegate status to filter by (true - only active, false - only deactivated, undefined - all delegates)</param>
        /// <param name="lastActivity">Filters delegates by last activity level (where the delegate was updated)</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `id` (default), `activationLevel`, `deactivationLevel`, `stakingBalance`, `balance`, `numDelegators`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Models.Delegate>>> Get(
            BoolParameter active,
            Int32Parameter lastActivity,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "stakingBalance", "balance", "numDelegators", "activationLevel", "deactivationLevel"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Accounts.GetDelegates(active, lastActivity, sort, offset, limit));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Accounts.GetDelegates(active, lastActivity, sort, offset, limit, select.Values[0]));
                else
                    return Ok(await Accounts.GetDelegates(active, lastActivity, sort, offset, limit, select.Values));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Accounts.GetDelegates(active, lastActivity, sort, offset, limit, select.Fields[0]));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Accounts.GetDelegates(active, lastActivity, sort, offset, limit, select.Fields)
                    });
                }
            }
        }

        /// <summary>
        /// Get delegates count
        /// </summary>
        /// <remarks>
        /// Returns a number of delegate accounts.
        /// </remarks>
        /// <param name="active">Delegate status to filter by (true - only active, false - only deactivated, undefined - all delegates)</param>
        /// <returns></returns>
        [HttpGet("count")]
        public Task<int> GetCount(BoolParameter active)
        {
            return Accounts.GetDelegatesCount(active);
        }

        /// <summary>
        /// Get delegate by address
        /// </summary>
        /// <remarks>
        /// Returns a delegate with the specified address.
        /// </remarks>
        /// <param name="address">Delegate address (starting with tz)</param>
        /// <returns></returns>
        [HttpGet("{address}")]
        public Task<Models.Delegate> GetByAddress([Required][TzAddress] string address)
        {
            return Accounts.GetDelegate(address);
        }
    }
}
