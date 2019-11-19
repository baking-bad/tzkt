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
    [Route("v1/[controller]")]
    public class BlocksController : ControllerBase
    {
        private readonly BlockRepository Blocks;
        public BlocksController(BlockRepository blocks)
        {
            Blocks = blocks;
        }

        [HttpGet("{level:int}")]
        public Task<Block> Get([Min(0)] int level, bool operations = false)
        {
            return Blocks.Get(level, operations);
        }

        [HttpGet("{hash}")]
        public Task<Block> Get([BlockHash] string hash, bool operations = false)
        {
            return Blocks.Get(hash, operations);
        }

        [HttpGet]
        public Task<IEnumerable<Block>> Get([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Blocks.Get(n, p * n);
        }
    }
}
