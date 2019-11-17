using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class ContractsController : ControllerBase
    {
        private readonly ContractRepository Contracts;
        public ContractsController(ContractRepository contracts)
        {
            Contracts = contracts;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Contract>>> Get(int p = 0, int n = 100)
        {
            if (n > 1000)
            {
                return BadRequest(new
                {
                    Code = "400",
                    Field = nameof(n),
                    Message = "Maximum number of items is 1000"
                });
            }

            return Ok(await Contracts.Get(n, p * n));
        }

        [HttpGet("{address}")]
        public async Task<ActionResult<Contract>> Get(string address)
        {
            if (!Regex.IsMatch(address, "^KT1[0-9A-z]{33}$"))
            {
                return BadRequest(new
                {
                    Code = "400",
                    Field = nameof(address),
                    Message = "Invalid KT1 address"
                });
            }

            return await Contracts.Get(address);
        }
    }
}
