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

        /// <summary>
        /// Get protocols
        /// </summary>
        /// <remarks>
        /// Returns a list of protocols.
        /// </remarks>
        /// <param name="p">Page offset (pagination)</param>
        /// <param name="n">Number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public Task<IEnumerable<Protocol>> Get([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Protocols.Get(n, p * n);
        }

        /// <summary>
        /// Get protocol by code
        /// </summary>
        /// <remarks>
        /// Returns a protocol with the specified proto code.
        /// </remarks>
        /// <param name="code">Protocol code (e.g. 4 for Athens, 5 for Babylon, etc)</param>
        /// <returns></returns>
        [HttpGet("{code:int}")]
        public Task<Protocol> GetByCode([Min(0)] int code)
        {
            return Protocols.Get(code);
        }

        /// <summary>
        /// Get protocol by hash
        /// </summary>
        /// <remarks>
        /// Returns a protocol with the specified hash.
        /// </remarks>
        /// <param name="hash">Protocol hash</param>
        /// <returns></returns>
        [HttpGet("{hash}")]
        public Task<Protocol> GetByHash([ProtocolHash] string hash)
        {
            return Protocols.Get(hash);
        }
    }
}
