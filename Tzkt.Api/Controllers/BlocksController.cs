using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/blocks")]
    public class BlocksController : ControllerBase
    {
        private readonly BlockRepository Blocks;
        public BlocksController(BlockRepository blocks)
        {
            Blocks = blocks;
        }

        /// <summary>
        /// Get blocks
        /// </summary>
        /// <remarks>
        /// Returns a list of blocks.
        /// </remarks>
        /// <param name="sort">Sorts blocks by specified field. Supported fields: `level`, `timestamp`, `priority`, `validations`, `reward`, `fees`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Block>>> Get(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("level", "timestamp", "priority", "validations", "reward", "fees"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Blocks.Get(sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Blocks.Get(sort, offset, limit, fields[0]));

            return Ok(await Blocks.Get(sort, offset, limit, fields));
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
