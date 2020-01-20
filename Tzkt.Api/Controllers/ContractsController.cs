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
    public class ContractsController : ControllerBase
    {
        private readonly AccountRepository Accounts;
        public ContractsController(AccountRepository accounts)
        {
            Accounts = accounts;
        }

        /// <summary>
        /// Get contracts
        /// </summary>
        /// <remarks>
        /// Returns a list of contract accounts.
        /// </remarks>
        /// <param name="kind">Contract kind to filter by (delegator_contract, smart_contract)</param>
        /// <param name="p">Page offset (pagination)</param>
        /// <param name="n">Number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public Task<IEnumerable<Contract>> Get([ContractKind] string kind, [Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            var kindValue = kind == null ? null : kind[0] == 'd' ? 0 : (int?)1;
            return Accounts.GetContracts(kindValue, n, p * n);
        }

        /// <summary>
        /// Get contract by address
        /// </summary>
        /// <remarks>
        /// Returns a contract account with the specified address.
        /// </remarks>
        /// <param name="address">Contract address (starting with KT)</param>
        /// <returns></returns>
        [HttpGet("{address}")]
        public Task<Contract> GetByAddress([Address] string address)
        {
            return Accounts.GetContract(address);
        }
    }
}
