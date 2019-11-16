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
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserRepository Users;
        public UsersController(UserRepository users)
        {
            Users = users;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> Get(int p = 0, int n = 100)
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

            return Ok(await Users.Get(n, p * n));
        }

        [HttpGet("{address}")]
        public async Task<ActionResult<User>> Get(string address)
        {
            if (!Regex.IsMatch(address, "^tz[0-9A-z]{34}$"))
            {
                return BadRequest(new
                {
                    Code = "400",
                    Field = nameof(address),
                    Message = "Invalid tz address"
                });
            }

            return await Users.Get(address);
        }
    }
}
