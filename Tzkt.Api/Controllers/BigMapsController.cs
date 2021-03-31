using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Netezos.Encoding;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/bigmaps")]
    public class BigMapsController : ControllerBase
    {
        private readonly BigMapsRepository BigMaps;

        public BigMapsController(BigMapsRepository bigMaps)
        {
            BigMaps = bigMaps;
        }

        /// <summary>
        /// Get bigmaps count
        /// </summary>
        /// <remarks>
        /// Returns the total number of bigmaps.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("count")]
        public Task<int> GetCount()
        {
            return BigMaps.GetCount();
        }

        /// <summary>
        /// Get bigmaps
        /// </summary>
        /// <remarks>
        /// Returns a list of bigmaps.
        /// </remarks>
        /// <param name="contract">Filters bigmaps by smart contract address.</param>
        /// <param name="active">Filters bigmaps by status: `true` - active, `false` - removed.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts bigmaps by specified field. Supported fields: `id` (default), `ptr`, `firstLevel`, `lastLevel`, `totalKeys`, `activeKeys`, `updates`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the bigmap key and value type: `0` - JSON, `2` - raw micheline</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BigMap>>> Get(
            AccountParameter contract,
            BoolParameter active,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "ptr", "firstLevel", "lastLevel", "totalKeys", "activeKeys", "updates"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await BigMaps.Get(contract, active, sort, offset, limit, micheline));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await BigMaps.Get(contract, active, sort, offset, limit, select.Values[0], micheline));
                else
                    return Ok(await BigMaps.Get(contract, active, sort, offset, limit, select.Values, micheline));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await BigMaps.Get(contract, active, sort, offset, limit, select.Fields[0], micheline));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await BigMaps.Get(contract, active, sort, offset, limit, select.Fields, micheline)
                    });
                }
            }
        }

        /// <summary>
        /// Get bigmap by ptr
        /// </summary>
        /// <remarks>
        /// Returns a bigmap with the specified ptr.
        /// </remarks>
        /// <param name="ptr">Bigmap pointer</param>
        /// <param name="micheline">Format of the bigmap key and value type: `0` - JSON, `2` - raw micheline</param>
        /// <returns></returns>
        [HttpGet("{ptr:int}")]
        public Task<BigMap> GetByPtr(
            [Min(0)] int ptr,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            return BigMaps.Get(ptr, micheline);
        }

        /// <summary>
        /// Get bigmap type
        /// </summary>
        /// <remarks>
        /// Returns a type of the bigmap with the specified ptr in Micheline format (with annotations).
        /// </remarks>
        /// <param name="ptr">Bigmap pointer</param>
        /// <returns></returns>
        [HttpGet("{ptr:int}/type")]
        public Task<MichelinePrim> GetTypeByPtr([Min(0)] int ptr)
        {
            return BigMaps.GetMicheType(ptr);
        }
    }
}
