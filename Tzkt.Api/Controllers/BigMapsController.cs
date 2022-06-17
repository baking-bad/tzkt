using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Netezos.Encoding;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/bigmaps")]
    public class BigMapsController : ControllerBase
    {
        readonly BigMapsRepository BigMaps;
        readonly ResponseCacheService ResponseCache;

        public BigMapsController(BigMapsRepository bigMaps, ResponseCacheService responseCache)
        {
            BigMaps = bigMaps;
            ResponseCache = responseCache;
        }

        /// <summary>
        /// Get bigmaps count
        /// </summary>
        /// <remarks>
        /// Returns the total number of bigmaps.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetBigMapsCount()
        {
            var query = Request.Path.Value;

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await BigMaps.GetCount();
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get bigmaps
        /// </summary>
        /// <remarks>
        /// Returns a list of bigmaps.
        /// </remarks>
        /// <param name="contract">Filters bigmaps by smart contract address.</param>
        /// <param name="path">Filters bigmaps by path in the contract storage.</param>
        /// <param name="tags">Filters bigmaps by tags: `metadata`, `token_metadata`, `ledger`.</param>
        /// <param name="active">Filters bigmaps by status: `true` - active, `false` - removed.</param>
        /// <param name="lastLevel">Filters bigmaps by the last update level.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts bigmaps by specified field. Supported fields: `id` (default), `ptr`, `firstLevel`, `lastLevel`, `totalKeys`, `activeKeys`, `updates`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the bigmap key and value type: `0` - JSON, `2` - Micheline</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BigMap>>> GetBigMaps(
            AccountParameter contract,
            StringParameter path,
            BigMapTagsParameter tags,
            bool? active,
            Int32Parameter lastLevel,
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

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("contract", contract), ("path", path), ("tags", tags), ("active", active), ("lastLevel", lastLevel),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("micheline", micheline));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await BigMaps.Get(contract, path, tags, active, lastLevel, sort, offset, limit, micheline);
            }
            else
            {
                if (select.Values != null)
                {
                    if (select.Values.Length == 1)
                        res = await BigMaps.Get(contract, path, tags, active, lastLevel, sort, offset, limit, select.Values[0], micheline);
                    else
                        res = await BigMaps.Get(contract, path, tags, active, lastLevel, sort, offset, limit, select.Values, micheline);
                }
                else
                {
                    if (select.Fields.Length == 1)
                        res = await BigMaps.Get(contract, path, tags, active, lastLevel, sort, offset, limit, select.Fields[0], micheline);
                    else
                    {
                        res = new SelectionResponse
                        {
                            Cols = select.Fields,
                            Rows = await BigMaps.Get(contract, path, tags, active, lastLevel, sort, offset, limit, select.Fields, micheline)
                        };
                    }
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get bigmap updates
        /// </summary>
        /// <remarks>
        /// Returns a list of all bigmap updates.
        /// </remarks>
        /// <param name="bigmap">Filters updates by bigmap ptr</param>
        /// <param name="path">Filters updates by bigmap path</param>
        /// <param name="contract">Filters updates by bigmap contract</param>
        /// <param name="tags">Filters updates by bigmap tags: `metadata`, `token_metadata`, `ledger`</param>
        /// <param name="action">Filters updates by action</param>
        /// <param name="value">Filters updates by JSON value. Note, this query parameter supports the following format: `?value{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?value.balance.gt=...`.</param>
        /// <param name="level">Filters updates by level</param>
        /// <param name="timestamp">Filters updates by timestamp.</param>
        /// <param name="sort">Sorts bigmaps by specified field. Supported fields: `id` (default), `ptr`, `firstLevel`, `lastLevel`, `totalKeys`, `activeKeys`, `updates`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the bigmap key and value type: `0` - JSON, `2` - Micheline</param>
        /// <returns></returns>
        [HttpGet("updates")]
        public async Task<ActionResult<IEnumerable<BigMapUpdate>>> GetBigMapUpdates(
            Int32Parameter bigmap,
            StringParameter path,
            AccountParameter contract,
            BigMapTagsParameter tags,
            BigMapActionParameter action,
            JsonParameter value,
            Int32Parameter level,
            TimestampParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("bigmap", bigmap), ("path", path), ("contract", contract), ("tags", tags),
                ("action", action), ("value", value), ("level", level), ("timestamp", timestamp),
                ("sort", sort), ("offset", offset), ("limit", limit), ("micheline", micheline));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (path == null && contract == null && tags == null)
            {
                res = await BigMaps.GetUpdates(bigmap, action, value, level, timestamp, sort, offset, limit, micheline);
            }
            else
            {
                res = await BigMaps.GetUpdates(bigmap, path, contract, action, value, tags, level, timestamp, sort, offset, limit, micheline);
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get bigmap by Id
        /// </summary>
        /// <remarks>
        /// Returns a bigmap with the specified Id.
        /// </remarks>
        /// <param name="id">Bigmap Id</param>
        /// <param name="micheline">Format of the bigmap key and value type: `0` - JSON, `2` - Micheline</param>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<BigMap>> GetBigMapById(
            [Min(0)] int id,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("micheline", micheline));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await BigMaps.Get(id, micheline);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get bigmap type
        /// </summary>
        /// <remarks>
        /// Returns a type of the bigmap with the specified Id in Micheline format (with annotations).
        /// </remarks>
        /// <param name="id">Bigmap Id</param>
        /// <returns></returns>
        [HttpGet("{id:int}/type")]
        public async Task<ActionResult<MichelinePrim>> GetBigMapType([Min(0)] int id)
        {
            var query = Request.Path.Value;

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await BigMaps.GetMicheType(id);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get bigmap keys
        /// </summary>
        /// <remarks>
        /// Returns a list of bigmap keys.
        /// </remarks>
        /// <param name="id">Bigmap Id</param>
        /// <param name="active">Filters keys by status: `true` - active, `false` - removed.</param>
        /// <param name="key">Filters keys by JSON key. Note, this query parameter supports the following format: `?key{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?key.token_id=...`.</param>
        /// <param name="value">Filters keys by JSON value. Note, this query parameter supports the following format: `?value{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?value.balance.gt=...`.</param>
        /// <param name="lastLevel">Filters bigmap keys by the last update level.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts bigmap keys by specified field. Supported fields: `id` (default), `firstLevel`, `lastLevel`, `updates`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the bigmap key and value: `0` - JSON, `1` - JSON string, `2` - Micheline, `3` - Micheline string</param>
        /// <returns></returns>
        [HttpGet("{id:int}/keys")]
        public async Task<ActionResult<IEnumerable<BigMapKey>>> GetKeys(
            [Min(0)] int id,
            bool? active,
            JsonParameter key,
            JsonParameter value,
            Int32Parameter lastLevel,
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

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("active", active), ("key", key), ("value", value), ("lastLevel", lastLevel),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("micheline", micheline));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await BigMaps.GetKeys(id, active, key, value, lastLevel, sort, offset, limit, micheline);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await BigMaps.GetKeys(id, active, key, value, lastLevel, sort, offset, limit, select.Values[0], micheline);
                else
                    res = await BigMaps.GetKeys(id, active, key, value, lastLevel, sort, offset, limit, select.Values, micheline);
            }
            else
            {
                if (select.Fields.Length == 1)
                    res = await BigMaps.GetKeys(id, active, key, value, lastLevel, sort, offset, limit, select.Fields[0], micheline);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await BigMaps.GetKeys(id, active, key, value, lastLevel, sort, offset, limit, select.Fields, micheline)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get bigmap key
        /// </summary>
        /// <remarks>
        /// Returns the specified bigmap key.
        /// </remarks>
        /// <param name="id">Bigmap Id</param>
        /// <param name="key">Either a key hash (`expr123...`) or a plain value (`abcde...`).
        /// Even if the key is complex (an object or an array), you can specify it as is, for example, `/keys/{"address":"tz123","nat":"123"}`.</param>
        /// <param name="micheline">Format of the bigmap key and value: `0` - JSON, `1` - JSON string, `2` - Micheline, `3` - Micheline string</param>
        /// <returns></returns>
        [HttpGet("{id:int}/keys/{key}")]
        public async Task<ActionResult<BigMapKey>> GetKey(
            [Min(0)] int id,
            [Required] string key,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            try
            {
                var query = ResponseCacheService.BuildKey(Request.Path.Value, ("micheline", micheline));

                if (ResponseCache.TryGet(query, out var cached))
                    return this.Bytes(cached);

                object res;
                if (Regex.IsMatch(key, @"^expr[0-9A-z]{50}$"))
                {
                    res = await BigMaps.GetKeyByHash(id, key, micheline);
                }
                else
                {
                    using var doc = JsonDocument.Parse(WrapKey(key));
                    res = await BigMaps.GetKey(id, doc.RootElement.GetRawText(), micheline);
                }
                cached = ResponseCache.Set(query, res);
                return this.Bytes(cached);
            }
            catch (JsonException)
            {
                return new BadRequest(nameof(key), "invalid json value");
            }
        }

        /// <summary>
        /// Get bigmap key updates
        /// </summary>
        /// <remarks>
        /// Returns updates history for the specified bigmap key.
        /// </remarks>
        /// <param name="id">Bigmap Id</param>
        /// <param name="key">Either a key hash (`expr123...`) or a plain value (`abcde...`).
        /// Even if the key is complex (an object or an array), you can specify it as is, for example, `/keys/{"address":"tz123","nat":"123"}`.</param>
        /// <param name="sort">Sorts bigmap updates by specified field. Supported fields: `id` (default).</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the key value: `0` - JSON, `1` - JSON string, `2` - Micheline, `3` - Micheline string</param>
        /// <returns></returns>
        [HttpGet("{id:int}/keys/{key}/updates")]
        public async Task<ActionResult<IEnumerable<BigMapKeyUpdate>>> GetKeyUpdates(
            [Min(0)] int id,
            [Required] string key,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            #region validate
            if (sort != null && !sort.Validate("id"))
                return new BadRequest(nameof(sort), "Sorting by the specified field is not allowed.");
            #endregion

            try
            {
                var query = ResponseCacheService.BuildKey(Request.Path.Value,
                    ("sort", sort), ("offset", offset), ("limit", limit), ("micheline", micheline));

                if (ResponseCache.TryGet(query, out var cached))
                    return this.Bytes(cached);

                object res;
                if (Regex.IsMatch(key, @"^expr[0-9A-z]{50}$"))
                {
                    res = await BigMaps.GetKeyByHashUpdates(id, key, sort, offset, limit, micheline);
                }
                else
                {
                    using var doc = JsonDocument.Parse(WrapKey(key));
                    res = await BigMaps.GetKeyUpdates(id, doc.RootElement.GetRawText(), sort, offset, limit, micheline);
                }
                cached = ResponseCache.Set(query, res);
                return this.Bytes(cached);
            }
            catch (JsonException)
            {
                return new BadRequest(nameof(key), "invalid json value");
            }
        }

        /// <summary>
        /// Get historical keys
        /// </summary>
        /// <remarks>
        /// Returns a list of bigmap keys at the specific block.
        /// </remarks>
        /// <param name="id">Bigmap Id</param>
        /// <param name="level">Level of the block at which you want to get bigmap keys</param>
        /// <param name="active">Filters keys by status: `true` - active, `false` - removed.</param>
        /// <param name="key">Filters keys by JSON key. Note, this query parameter supports the following format: `?key{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?key.token_id=...`.</param>
        /// <param name="value">Filters keys by JSON value. Note, this query parameter supports the following format: `?value{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?value.balance.gt=...`.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts bigmap keys by specified field. Supported fields: `id` (default).</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the bigmap key and value: `0` - JSON, `1` - JSON string, `2` - Micheline, `3` - Micheline string</param>
        /// <returns></returns>
        [HttpGet("{id:int}/historical_keys/{level:int}")]
        public async Task<ActionResult<IEnumerable<BigMapKeyHistorical>>> GetHistoricalKeys(
            [Min(0)] int id,
            [Min(0)] int level,
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
            if (sort != null && !sort.Validate("id"))
                return new BadRequest(nameof(sort), "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("active", active), ("key", key), ("value", value),
                ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("micheline", micheline));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await BigMaps.GetHistoricalKeys(id, level, active, key, value, sort, offset, limit, micheline);
            }
            else
            {
                if (select.Values != null)
                {
                    if (select.Values.Length == 1)
                        res = await BigMaps.GetHistoricalKeys(id, level, active, key, value, sort, offset, limit, select.Values[0], micheline);
                    else
                        res = await BigMaps.GetHistoricalKeys(id, level, active, key, value, sort, offset, limit, select.Values, micheline);
                }
                else
                {
                    if (select.Fields.Length == 1)
                        res = await BigMaps.GetHistoricalKeys(id, level, active, key, value, sort, offset, limit, select.Fields[0], micheline);
                    else
                    {
                        res = new SelectionResponse
                        {
                            Cols = select.Fields,
                            Rows = await BigMaps.GetHistoricalKeys(id, level, active, key, value, sort, offset, limit, select.Fields, micheline)
                        };
                    }
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get historical key
        /// </summary>
        /// <remarks>
        /// Returns the specified bigmap key at the specific block.
        /// </remarks>
        /// <param name="id">Bigmap Id</param>
        /// <param name="level">Level of the block at which you want to get bigmap key</param>
        /// <param name="key">Either a key hash (`expr123...`) or a plain value (`abcde...`).
        /// Even if the key is complex (an object or an array), you can specify it as is, for example, `/keys/{"address":"tz123","nat":"123"}`.</param>
        /// <param name="micheline">Format of the bigmap key and value: `0` - JSON, `1` - JSON string, `2` - Micheline, `3` - Micheline string</param>
        /// <returns></returns>
        [HttpGet("{id:int}/historical_keys/{level:int}/{key}")]
        public async Task<ActionResult<BigMapKeyHistorical>> GetKey(
            [Min(0)] int id,
            [Min(0)] int level,
            [Required] string key,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            try
            {
                var query = ResponseCacheService.BuildKey(Request.Path.Value, ("micheline", micheline));

                if (ResponseCache.TryGet(query, out var cached))
                    return this.Bytes(cached);

                object res;
                if (Regex.IsMatch(key, @"^expr[0-9A-z]{50}$"))
                {
                    res = await BigMaps.GetHistoricalKeyByHash(id, level, key, micheline);
                }
                else
                {
                    using var doc = JsonDocument.Parse(WrapKey(key));
                    res = await BigMaps.GetHistoricalKey(id, level, doc.RootElement.GetRawText(), micheline);
                }
                cached = ResponseCache.Set(query, res);
                return this.Bytes(cached);
            }
            catch (JsonException)
            {
                return new BadRequest(nameof(key), "invalid json value");
            }
        }

        static string WrapKey(string key)
        {
            switch (key[0])
            {
                case '{':
                case '[':
                case '"':
                case 't' when key == "true":
                case 'f' when key == "false":
                case 'n' when key == "null":
                    return key;
                default:
                    return $"\"{key}\"";
            }
        }
    }
}
