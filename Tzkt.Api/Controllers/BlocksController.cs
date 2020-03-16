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
        /// <param name="p">Page offset (pagination)</param>
        /// <param name="n">Number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public Task<IEnumerable<Block>> Get([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Blocks.Get(n, p * n);
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
        public Task<IEnumerable<int>> GetSmartContractBlocks([Min(0)] int offset = 0, [Range(0, 10000)] int limit = 10000)
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
