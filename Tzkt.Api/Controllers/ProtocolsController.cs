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
    [Route("v1/protocols")]
    public class ProtocolsController : ControllerBase
    {
        private readonly ProtocolRepository Protocols;
        public ProtocolsController(ProtocolRepository protocols)
        {
            Protocols = protocols;
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
            return Protocols.GetCount();
        }

        /// <summary>
        /// Get protocols
        /// </summary>
        /// <remarks>
        /// Returns a list of protocols.
        /// </remarks>
        /// <param name="sort">Sorts protocols by specified field. Supported fields: `code`, `firstLevel`, `lastLevel`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Protocol>>> Get(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("code", "firstLevel", "lastLevel"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            return Ok(await Protocols.Get(sort, offset, limit));
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
