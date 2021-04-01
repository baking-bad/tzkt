using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
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
            bool? active,
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

        /// <summary>
        /// Get bigmap keys
        /// </summary>
        /// <remarks>
        /// Returns a list of bigmap keys.
        /// </remarks>
        /// <param name="ptr">Bigmap pointer</param>
        /// <param name="active">Filters keys by status: `true` - active, `false` - removed.</param>
        /// <param name="key">Filters keys by JSON key. Note, this query parameter supports the following format: `?key{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?key.token_id=...`.</param>
        /// <param name="value">Filters keys by JSON value. Note, this query parameter supports the following format: `?value{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?value.balance.gt=...`.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts bigmaps by specified field. Supported fields: `id` (default), `firstLevel`, `lastLevel`, `updates`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the bigmap key and value: `0` - JSON, `1` - JSON string, `2` - micheline, `3` - micheline string</param>
        /// <returns></returns>
        [HttpGet("{ptr:int}/keys")]
        public async Task<ActionResult<IEnumerable<BigMapKey>>> GetKeys(
            [Min(0)] int ptr,
            bool? active,
            JsonParameter key,
            JsonParameter value,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "firstLevel", "lastLevel", "updates"))
                return new BadRequest(nameof(sort), "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await BigMaps.GetKeys(ptr, active, key, value, sort, offset, limit, micheline));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await BigMaps.GetKeys(ptr, active, key, value, sort, offset, limit, select.Values[0], micheline));
                else
                    return Ok(await BigMaps.GetKeys(ptr, active, key, value, sort, offset, limit, select.Values, micheline));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await BigMaps.GetKeys(ptr, active, key, value, sort, offset, limit, select.Fields[0], micheline));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await BigMaps.GetKeys(ptr, active, key, value, sort, offset, limit, select.Fields, micheline)
                    });
                }
            }
        }

        /// <summary>
        /// Get bigmap key
        /// </summary>
        /// <remarks>
        /// Returns a bigmap key with the specified key value.
        /// </remarks>
        /// <param name="ptr">Bigmap pointer</param>
        /// <param name="key">Plain key, for example, `.../keys/abcde`.
        /// If the key is complex (an object or an array), you can specify it as is, for example, `.../keys/{"address":"tz123","token":123}`.</param>
        /// <param name="micheline">Format of the bigmap key and value: `0` - JSON, `1` - JSON string, `2` - micheline, `3` - micheline string</param>
        /// <returns></returns>
        [HttpGet("{ptr:int}/keys/{key}")]
        public async Task<ActionResult<BigMapKey>> GetKey(
            [Min(0)] int ptr,
            string key,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            try
            {
                switch (key[0])
                {
                    case '{':
                    case '[':
                    case '"':
                    case 't' when key == "true":
                    case 'f' when key == "false":
                    case 'n' when key == "null":
                        break;
                    default:
                        key = $"\"{key}\"";
                        break;
                }
                using var doc = JsonDocument.Parse(key);
                return Ok(await BigMaps.GetKey(ptr, doc.RootElement.GetRawText(), micheline));
            }
            catch (JsonException)
            {
                return new BadRequest(nameof(key), "invalid json value");
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Get bigmap key by hash
        /// </summary>
        /// <remarks>
        /// Returns a bigmap key with the specified key hash.
        /// </remarks>
        /// <param name="ptr">Bigmap pointer</param>
        /// <param name="hash">Key hash</param>
        /// <param name="micheline">Format of the bigmap key and value: `0` - JSON, `1` - JSON string, `2` - micheline, `3` - micheline string</param>
        /// <returns></returns>
        [HttpGet("{ptr:int}/keys/{hash:regex(^expr[[0-9A-z]]{{50}}$)}")]
        public Task<BigMapKey> GetKeyByHash(
            [Min(0)] int ptr,
            string hash,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            return BigMaps.GetKeyByHash(ptr, hash, micheline);
        }
    }
}
