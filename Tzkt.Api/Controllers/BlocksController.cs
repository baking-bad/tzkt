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
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts blocks by specified field. Supported fields: `id` (default), `level`, `priority`, `validations`, `reward`, `fees`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Block>>> Get(
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "level", "priority", "validations", "reward", "fees"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Blocks.Get(sort, offset, limit));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Blocks.Get(sort, offset, limit, select.Values[0]));
                else
                    return Ok(await Blocks.Get(sort, offset, limit, select.Values));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Blocks.Get(sort, offset, limit, select.Fields[0]));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Blocks.Get(sort, offset, limit, select.Fields)
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
        /// <returns></returns>
        [HttpGet("{hash}")]
        public Task<Block> GetByHash([BlockHash] string hash, bool operations = false)
        {
            return Blocks.Get(hash, operations);
        }

        /// <summary>
        /// Get block by level
        /// </summary>
        /// <remarks>
        /// Returns a block at the specified level.
        /// </remarks>
        /// <param name="level">Block level</param>
        /// <param name="operations">Flag indicating whether to include block operations into returned object or not</param>
        /// <returns></returns>
        [HttpGet("{level:int}")]
        public Task<Block> GetByLevel([Min(0)] int level, bool operations = false)
        {
            return Blocks.Get(level, operations);
        }

        [OpenApiIgnore]
        [HttpGet("levels")]
        public Task<IEnumerable<int>> GetSmartContractBlocks(OffsetParameter offset, [Range(0, 10000)] int limit = 10000)
        {
            return Blocks.GetEventLevels(Data.Models.BlockEvents.SmartContracts, offset, limit);
        }

        [OpenApiIgnore]
        [HttpGet("timestamps")]
        public Task<IEnumerable<DateTime>> GetTimestamps([Min(0)] int offset = 0, [Range(0, 10000)] int limit = 10000)
        {
            return Blocks.GetTimestamps(offset, limit);
        }
    }
}
