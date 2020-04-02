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
        /// Returns a list of accounts.
        /// </remarks>
        /// <param name="active">Delegate status to filter by (true - only active, false - only deactivated, undefined - all delegates)</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `activationLevel`, `deactivationLevel`, `stakingBalance`, `balance`, `numDelegators`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IEnumerable<Models.Delegate>))]
        public async Task<ActionResult> Get(
            BoolParameter active,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("stakingBalance", "balance", "numDelegators", "activationLevel", "deactivationLevel"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not supported.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Accounts.GetDelegates(active, sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Accounts.GetDelegates(active, sort, offset, limit, fields[0]));

            return Ok(await Accounts.GetDelegates(active, sort, offset, limit, fields));
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
        public Task<Models.Delegate> GetByAddress([Address] string address)
        {
            return Accounts.GetDelegate(address);
        }
    }
}
