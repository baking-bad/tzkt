using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
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
        public Task<IEnumerable<Contract>> Get([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Contracts.Get(n, p * n);
        }

        [HttpGet("{address}")]
        public Task<Contract> Get([KT1Address] string address)
        {
            return Contracts.Get(address);
        }
    }
}
