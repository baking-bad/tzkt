using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/blocks")]
    public class BlocksController : ControllerBase
    {
        private readonly BlockRepository Blocks;
        private readonly StateCache State;
        private readonly TimeCache Time;

        public BlocksController(BlockRepository blocks, StateCache state, TimeCache time)
        {
            Blocks = blocks;
            State = state;
            Time = time;
        }

        /// <summary>
        /// Get blocks count
        /// </summary>
        /// <remarks>
        /// Returns the total number of blocks.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("count")]
        public Task<int> GetCount()
        {
            return Task.FromResult(State.Current.BlocksCount);
        }

        /// <summary>
        /// Get blocks
        /// </summary>
        /// <remarks>
        /// Returns a list of blocks.
        /// </remarks>
        /// <param name="baker">[DEPRECATED]</param>
        /// <param name="anyof">Filters by any of the specified fields. Example: `anyof.proposer.producer=tz1...`.</param>
        /// <param name="proposer">Filters blocks by block proposer. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="producer">Filters blocks by block producer. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters blocks by level.</param>
        /// <param name="timestamp">Filters blocks by timestamp.</param>
        /// <param name="priority">[DEPRECATED]</param>
        /// <param name="blockRound">Filters blocks by block round.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts blocks by specified field. Supported fields: `id` (default), `level`, `payloadRound`, `blockRound`, `validations`, `reward`, `bonus`, `fees`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Block>>> Get(
            AccountParameter baker,
            [OpenApiExtensionData("x-tzkt-extension", "anyof-parameter")]
            [OpenApiExtensionData("x-tzkt-anyof-parameter", "proposer,producer")]
            AnyOfParameter anyof,
            AccountParameter proposer,
            AccountParameter producer,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter priority,
            Int32Parameter blockRound,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region deprecated
            producer ??= baker;
            blockRound ??= priority;
            #endregion

            #region validate
            if (anyof != null)
            {
                if (anyof.Fields.Any(x => x != "proposer" && x != "producer"))
                    return new BadRequest($"{nameof(anyof)}", "This parameter can be used with `proposer`, `producer` fields only.");

                if (anyof.Eq == -1 || anyof.In?.Count == 0 || anyof.Null == true)
                    return Ok(Enumerable.Empty<Block>());
            }
            if (proposer != null)
            {
                if (proposer.Eqx != null)
                    return new BadRequest($"{nameof(proposer)}.eqx", "This parameter doesn't support .eqx mode.");

                if (proposer.Nex != null)
                    return new BadRequest($"{nameof(proposer)}.nex", "This parameter doesn't support .nex mode.");

                if (proposer.Eq == -1 || proposer.In?.Count == 0 && !proposer.InHasNull)
                    return Ok(Enumerable.Empty<Block>());
            }
            if (producer != null)
            {
                if (producer.Eqx != null)
                    return new BadRequest($"{nameof(producer)}.eqx", "This parameter doesn't support .eqx mode.");

                if (producer.Nex != null)
                    return new BadRequest($"{nameof(producer)}.nex", "This parameter doesn't support .nex mode.");

                if (producer.Eq == -1 || producer.In?.Count == 0 && !producer.InHasNull)
                    return Ok(Enumerable.Empty<Block>());
            }

            if (sort != null && !sort.Validate("id", "level", "payloadRound", "blockRound", "validations", "reward", "bonus", "fees"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Blocks.Get(anyof, proposer, producer, level, timestamp, blockRound, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Blocks.Get(anyof, proposer, producer, level, timestamp, blockRound, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Blocks.Get(anyof, proposer, producer, level, timestamp, blockRound, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Blocks.Get(anyof, proposer, producer, level, timestamp, blockRound, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Blocks.Get(anyof, proposer, producer, level, timestamp, blockRound, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get block by hash
        /// </summary>
        /// <remarks>
        /// Returns a block with the specified hash.
        /// </remarks>
        /// <param name="hash">Block hash</param>
        /// <param name="operations">Flag indicating whether to include block operations into returned object or not</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{hash}")]
        public Task<Block> GetByHash(
            [Required][BlockHash] string hash,
            bool operations = false,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            return Blocks.Get(hash, operations, micheline, quote);
        }

        /// <summary>
        /// Get block by level
        /// </summary>
        /// <remarks>
        /// Returns a block at the specified level.
        /// </remarks>
        /// <param name="level">Block level</param>
        /// <param name="operations">Flag indicating whether to include block operations into returned object or not</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{level:int}")]
        public Task<Block> GetByLevel(
            [Min(0)] int level,
            bool operations = false,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            return Blocks.Get(level, operations, micheline, quote);
        }

        /// <summary>
        /// Get timestamp by level
        /// </summary>
        /// <remarks>
        /// Returns a timestamp of the block at the specified level.
        /// </remarks>
        /// <param name="level">Block level</param>
        /// <returns></returns>
        [HttpGet("{level:int}/timestamp")]
        public DateTime GetByLevel([Min(0)] int level)
        {
            return Time[level];
        }

        /// <summary>
        /// Get block by timestamp
        /// </summary>
        /// <remarks>
        /// Returns a block closest to the specified timestamp.
        /// </remarks>
        /// <param name="timestamp">Timestamp, e.g. `2020-01-01T00:00:00Z`</param>
        /// <param name="operations">Flag indicating whether to include block operations into returned object or not</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{timestamp:DateTime}")]
        public Task<Block> GetByDate(
            DateTimeOffset timestamp,
            bool operations = false,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            var level = Time.FindLevel(timestamp.UtcDateTime, SearchMode.ExactOrLower);
            return Blocks.Get(level, operations, micheline, quote);
        }

        /// <summary>
        /// Get level by timestamp
        /// </summary>
        /// <remarks>
        /// Returns a level of the block closest to the specified timestamp.
        /// </remarks>
        /// <param name="timestamp">Timestamp, e.g. `2020-01-01T00:00:00Z`</param>
        /// <returns></returns>
        [HttpGet("{timestamp:DateTime}/level")]
        public int GetByDate(DateTimeOffset timestamp)
        {
            return Time.FindLevel(timestamp.UtcDateTime, SearchMode.ExactOrLower);
        }

        // BCD bootstrap
        [OpenApiIgnore]
        [HttpGet("levels")]
        public Task<IEnumerable<int>> GetSpecificBlocks(
            bool? smartContracts,
            bool? delegatorContracts,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 10000)
        {
            var events = Data.Models.BlockEvents.None;
            if (smartContracts == true) events |= Data.Models.BlockEvents.SmartContracts;
            if (delegatorContracts == true) events |= Data.Models.BlockEvents.DelegatorContracts;

            return Blocks.GetEventLevels(events, offset, limit);
        }
    }
}
