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
    public class DelegatesController : ControllerBase
    {
        private readonly DelegateRepository Delegates;
        public DelegatesController(DelegateRepository delegates)
        {
            Delegates = delegates;
        }

        [HttpGet]
        public Task<IEnumerable<Models.Delegate>> Get([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Delegates.Get(n, p * n);
        }

        [HttpGet("{address}")]
        public Task<Models.Delegate> Get([TzAddress] string address)
        {
            return Delegates.Get(address);
        }
    }
}
