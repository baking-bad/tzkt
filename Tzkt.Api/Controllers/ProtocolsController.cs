using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/protocols")]
    public class ProtocolsController : ControllerBase
    {
        private readonly ProtocolRepository Protocols;
        private readonly StateCache State;

        public ProtocolsController(ProtocolRepository protocols, StateCache state)
        {
            Protocols = protocols;
            State = state;
        }
        /// <summary>
        /// Get protocols count
        /// </summary>
        /// <remarks>
        /// Returns the total number of protocols.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("count")]
        public Task<int> GetCount()
        {
            return Task.FromResult(State.Current.ProtocolsCount);
        }

        /// <summary>
        /// Get protocols
        /// </summary>
        /// <remarks>
        /// Returns a list of protocols.
        /// </remarks>
        /// <param name="sort">Sorts protocols by specified field. Supported fields: `code` (default), `firstLevel`, `lastLevel`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Protocol>>> Get(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "code", "firstLevel", "lastLevel"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            return Ok(await Protocols.Get(sort, offset, limit));
        }

        /// <summary>
        /// Get current protocol
        /// </summary>
        /// <remarks>
        /// Returns current protocol.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("current")]
        public Task<Protocol> GetCurrent()
        {
            return Protocols.Get(State.Current.Protocol);
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
        public Task<Protocol> GetByHash([Required][ProtocolHash] string hash)
        {
            return Protocols.Get(hash);
        }

        /// <summary>
        /// Get protocol by cycle
        /// </summary>
        /// <remarks>
        /// Returns a protocol at the specified cycle.
        /// </remarks>
        /// <param name="cycle">Cycle index</param>
        /// <returns></returns>
        [HttpGet("cycles/{cycle}")]
        public Task<Protocol> GetByCycle([Min(0)] int cycle)
        {
            return Protocols.GetByCycle(cycle);
        }
    }
}
