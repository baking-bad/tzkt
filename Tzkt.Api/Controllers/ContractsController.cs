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
    [Route("v1/contracts")]
    public class ContractsController : ControllerBase
    {
        private readonly AccountRepository Accounts;
        public ContractsController(AccountRepository accounts)
        {
            Accounts = accounts;
        }

        /// <summary>
        /// Get contracts
        /// </summary>
        /// <remarks>
        /// Returns a list of contract accounts.
        /// </remarks>
        /// <param name="kind">Contract kind to filter by (`delegator_contract` or `smart_contract`)</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.rec` and `.tup` modes.</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `balance`, `firstActivity`, `lastActivity`, `numTransactions`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Contract>>> Get(
            ContractKindParameter kind,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("balance", "firstActivity", "lastActivity", "numTransactions"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Accounts.GetContracts(kind, sort, offset, limit));

            if (select.Tup != null)
            {
                if (select.Tup.Length == 1)
                    return Ok(await Accounts.GetContracts(kind, sort, offset, limit, select.Tup[0]));
                else
                    return Ok(await Accounts.GetContracts(kind, sort, offset, limit, select.Tup));
            }
            else
            {
                if (select.Rec.Length == 1)
                    return Ok(await Accounts.GetContracts(kind, sort, offset, limit, select.Rec[0]));
                else
                {
                    return Ok(new SelectionResponse
                    {
                         Cols = select.Rec,
                         Rows = await Accounts.GetContracts(kind, sort, offset, limit, select.Rec)
                    });
                }
            }
        }

        /// <summary>
        /// Get contracts count
        /// </summary>
        /// <remarks>
        /// Returns a number of contract accounts.
        /// </remarks>
        /// <param name="kind">Contract kind to filter by (`delegator_contract` or `smart_contract`)</param>
        /// <returns></returns>
        [HttpGet("count")]
        public Task<int> GetCount(ContractKindParameter kind)
        {
            return Accounts.GetContractsCount(kind);
        }

        /// <summary>
        /// Get contract by address
        /// </summary>
        /// <remarks>
        /// Returns a contract account with the specified address.
        /// </remarks>
        /// <param name="address">Contract address (starting with KT)</param>
        /// <returns></returns>
        [HttpGet("{address}")]
        public Task<Contract> GetByAddress([Address] string address)
        {
            return Accounts.GetContract(address);
        }
    }
}
