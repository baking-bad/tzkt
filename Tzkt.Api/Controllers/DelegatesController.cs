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
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `activationLevel`, `deactivationLevel`, `stakingBalance`, `balance`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Models.Delegate>>> Get(
            BoolParameter active,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null)
            {
                if (sort.Asc != null && !(sort.Asc == "activationLevel" || sort.Asc == "stakingBalance" || sort.Asc == "deactivationLevel" || sort.Asc == "balance"))
                    return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not supported.");

                if (sort.Desc != null && !(sort.Desc == "activationLevel" || sort.Desc == "stakingBalance" || sort.Desc == "deactivationLevel" || sort.Desc == "balance"))
                    return new BadRequest($"{nameof(sort)}.desc", "Sorting by the specified field is not supported.");
            }
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            return Ok(await Accounts.GetDelegates(active, sort, offset, limit));
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
