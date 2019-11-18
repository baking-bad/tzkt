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
        public Task<IEnumerable<Account>> Get([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Accounts.Get(n, p * n);
        }

        [HttpGet("{address}")]
        public Task<Account> Get([Address] string address)
        {
            return Accounts.Get(address);
        }
    }
}
