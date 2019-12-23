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
    public class DelegatesController : ControllerBase
    {
        private readonly AccountRepository Accounts;
        public DelegatesController(AccountRepository accounts)
        {
            Accounts = accounts;
        }

        [HttpGet]
        public Task<IEnumerable<Models.Delegate>> Get(bool? active, [Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Accounts.GetDelegates(active, n, p * n);
        }

        [HttpGet("{address}")]
        public Task<Models.Delegate> Get([Address] string address)
        {
            return Accounts.GetDelegate(address);
        }
    }
}
