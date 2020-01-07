using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Metadata;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class ContractsController : ControllerBase
    {
        private readonly AccountRepository Accounts;
        public ContractsController(AccountRepository accounts)
        {
            Accounts = accounts;
        }

        [HttpGet]
        public Task<IEnumerable<Contract>> Get([ContractKind] string kind, [Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            var kindValue = kind == null ? null : kind[0] == 'd' ? 0 : (int?)1;
            return Accounts.GetContracts(kindValue, n, p * n);
        }

        [HttpGet("{address}")]
        public Task<Contract> Get([Address] string address)
        {
            return Accounts.GetContract(address);
        }
    }
}
