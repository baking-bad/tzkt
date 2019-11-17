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
    public class AccountsController : ControllerBase
    {
        private readonly AccountRepository Accounts;
        public AccountsController(AccountRepository accounts)
        {
            Accounts = accounts;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Account>>> Get(int p = 0, int n = 100)
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

            return Ok(await Accounts.Get(n, p * n));
        }

        [HttpGet("{address}")]
        public async Task<ActionResult<Account>> Get(string address)
        {
            if (!Regex.IsMatch(address, "^[0-9A-z]{36}$"))
            {
                return BadRequest(new
                {
                    Code = "400",
                    Field = nameof(address),
                    Message = "Invalid account address"
                });
            }

            return await Accounts.Get(address);
        }
    }
}
