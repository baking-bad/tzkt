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
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `id` (default), `balance`, `firstActivity`, `lastActivity`, `numTransactions`.</param>
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
            if (sort != null && !sort.Validate("id", "balance", "firstActivity", "lastActivity", "numTransactions"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Accounts.GetContracts(kind, sort, offset, limit));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Accounts.GetContracts(kind, sort, offset, limit, select.Values[0]));
                else
                    return Ok(await Accounts.GetContracts(kind, sort, offset, limit, select.Values));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Accounts.GetContracts(kind, sort, offset, limit, select.Fields[0]));
                else
                {
                    return Ok(new SelectionResponse
                    {
                         Cols = select.Fields,
                         Rows = await Accounts.GetContracts(kind, sort, offset, limit, select.Fields)
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

        /// <summary>
        /// Get contract code
        /// </summary>
        /// <remarks>
        /// Returns a code of the specified contract.
        /// </remarks>
        /// <param name="address">Contract address (starting with KT)</param>
        /// <param name="format">Code format (`0` - micheline, `1` - michelson, `2` - bytes (base64))</param>
        /// <returns></returns>
        [HttpGet("{address}/code")]
        public async Task<object> GetCode([Address] string address, [Range(0, 2)] int format = 0)
        {
            if (format == 0)
                return await Accounts.GetMichelineCode(address);
            else if (format == 1)
                return await Accounts.GetMichelsonCode(address);
            return await Accounts.GetByteCode(address);
        }

        /// <summary>
        /// Get contract entrypoints
        /// </summary>
        /// <remarks>
        /// Returns entrypoints of the specified contract.
        /// </remarks>
        /// <param name="address">Contract address (starting with KT)</param>
        /// <param name="all">If true, returns all entrypoints, including unused ones.
        /// Unused means that the entrypoint can be normalized to a more specific one.
        /// For example here `(or %entry1 (unit %entry2) (nat %entry3))` the `%entry1` is unused entrypoint
        /// because it can be normalized to `%entry2` or `%entry3`</param>
        /// <param name="json">Include parameters schema in human-readable JSON format</param>
        /// <param name="micheline">Include parameters schema in micheline format</param>
        /// <param name="michelson">Include parameters schema in michelson format</param>
        /// <returns></returns>
        [HttpGet("{address}/entrypoints")]
        public Task<IEnumerable<Entrypoint>> GetEntrypoints([Address] string address, bool all = false, bool json = true, bool micheline = false, bool michelson = false)
        {
            return Accounts.GetEntrypoints(address, all, json, micheline, michelson);
        }

        /// <summary>
        /// Get entrypoint by name
        /// </summary>
        /// <remarks>
        /// Returns contract's entrypoint with specified name.
        /// </remarks>
        /// <param name="address">Contract address (starting with KT)</param>
        /// <param name="name">Entrypoint name</param>
        /// <param name="json">Include parameters schema in human-readable JSON format</param>
        /// <param name="micheline">Include parameters schema in micheline format</param>
        /// <param name="michelson">Include parameters schema in michelson format</param>
        /// <returns></returns>
        [HttpGet("{address}/entrypoints/{name}")]
        public Task<Entrypoint> GetEntrypointByName([Address] string address, string name, bool json = true, bool micheline = false, bool michelson = false)
        {
            return Accounts.GetEntrypoint(address, name, json, micheline, michelson);
        }
    }
}
