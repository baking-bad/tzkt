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
    public class AccountsController : ControllerBase
    {
        private readonly AccountRepository Accounts;
        public AccountsController(AccountRepository accounts)
        {
            Accounts = accounts;
        }

        [HttpGet]
        public Task<IEnumerable<IAccount>> Get([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Accounts.Get(n, p * n);
        }

        [HttpGet("{address}")]
        public Task<IAccount> Get([Address] string address)
        {
            return Accounts.Get(address);
        }

        [HttpGet("{address}/profile")]
        public Task<IAccount> GetProfile([Address] string address)
        {
            return Accounts.GetProfile(address);
        }

        [HttpGet("{address}/delegators")]
        public Task<IEnumerable<DelegatorInfo>> GetDelegators([Address] string address, [Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Accounts.GetDelegators(address, n, p * n);
        }

        [HttpGet("{address}/operations")]
        public Task<IEnumerable<IOperation>> GetOperations([Address] string address, Data.Models.Operations? mask, [Min(0)] int from = 0, [Range(0, 1000)] int n = 100)
        {
            mask ??= Data.Models.Operations.All &
                ~Data.Models.Operations.Endorsements &
                ~Data.Models.Operations.Revelations;

            return from == 0
                ? Accounts.GetOperations(address, mask.Value, n)
                : Accounts.GetOperations(address, mask.Value, from, n);
        }
    }
}
