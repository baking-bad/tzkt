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
    public class ProtocolsController : ControllerBase
    {
        private readonly ProtocolRepository Protocols;
        public ProtocolsController(ProtocolRepository protocols)
        {
            Protocols = protocols;
        }

        [HttpGet("{code:int}")]
        public Task<Protocol> Get([Min(0)] int code)
        {
            return Protocols.Get(code);
        }

        [HttpGet("{hash}")]
        public Task<Protocol> Get([ProtocolHash] string hash)
        {
            return Protocols.Get(hash);
        }

        [HttpGet]
        public Task<IEnumerable<Protocol>> Get([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Protocols.Get(n, p * n);
        }
    }
}
