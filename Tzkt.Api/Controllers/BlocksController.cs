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

        public BlocksController(BlockRepository blocks, StateCache state)
        {
            Blocks = blocks;
            State = state;
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
            return Task.FromResult(State.GetState().BlocksCount);
        }

        /// <summary>
        /// Get blocks
        /// </summary>
        /// <remarks>
        /// Returns a list of blocks.
        /// </remarks>
        /// <param name="baker">Filters blocks by baker. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters blocks by level.</param>
        /// <param name="priority">Filters blocks by priority.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts blocks by specified field. Supported fields: `id` (default), `level`, `priority`, `validations`, `reward`, `fees`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Block>>> Get(
            AccountParameter baker,
            Int32Parameter level,
            Int32Parameter priority,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (baker != null)
            {
                if (baker.Eqx != null)
                    return new BadRequest($"{nameof(baker)}.eqx", "This parameter doesn't support .eqx mode.");

                if (baker.Nex != null)
                    return new BadRequest($"{nameof(baker)}.nex", "This parameter doesn't support .nex mode.");

                if (baker.Eq == -1 || baker.In?.Count == 0)
                    return Ok(Enumerable.Empty<OriginationOperation>());
            }

            if (sort != null && !sort.Validate("id", "level", "priority", "validations", "reward", "fees"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Blocks.Get(baker, level, priority, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Blocks.Get(baker, level, priority, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await Blocks.Get(baker, level, priority, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Blocks.Get(baker, level, priority, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Blocks.Get(baker, level, priority, sort, offset, limit, select.Fields, quote)
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
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{hash}")]
        public Task<Block> GetByHash([BlockHash] string hash, bool operations = false, Symbols quote = Symbols.None)
        {
            return Blocks.Get(hash, operations, quote);
        }

        /// <summary>
        /// Get block by level
        /// </summary>
        /// <remarks>
        /// Returns a block at the specified level.
        /// </remarks>
        /// <param name="level">Block level</param>
        /// <param name="operations">Flag indicating whether to include block operations into returned object or not</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{level:int}")]
        public Task<Block> GetByLevel([Min(0)] int level, bool operations = false, Symbols quote = Symbols.None)
        {
            return Blocks.Get(level, operations, quote);
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
