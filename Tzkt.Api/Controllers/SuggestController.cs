using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/suggest")]
    public class SuggestController : ControllerBase
    {
        readonly AliasesCache Aliases;

        public SuggestController(AliasesCache aliases)
        {
            Aliases = aliases;
        }

        [OpenApiIgnore]
        [HttpGet("accounts")]
        public IEnumerable<Alias> GetAccounts()
        {
            return Aliases.Aliases;
        }

        [OpenApiIgnore]
        [HttpGet("accounts/{search}")]
        public IEnumerable<Alias> GetAccounts([Required] string search, [Range(0, 100)] int limit = 20)
        {
            return Aliases.Search(search, limit);
        }

        [OpenApiIgnore]
        [HttpGet("proposals")]
        public IEnumerable<object> GetProposals()
        {
            // obsolete
            return Enumerable.Empty<object>();
        }

        [OpenApiIgnore]
        [HttpGet("proposals/{search}")]
        public IEnumerable<object> GetProposals([Required] string search)
        {
            // obsolete
            return Enumerable.Empty<object>();
        }

        [OpenApiIgnore]
        [HttpGet("protocols")]
        public IEnumerable<object> GetProtocols()
        {
            // obsolete
            return Enumerable.Empty<object>();
        }

        [OpenApiIgnore]
        [HttpGet("protocols/{search}")]
        public IEnumerable<object> GetProtocols([Required] string search)
        {
            // obsolete
            return Enumerable.Empty<object>();
        }
    }
}
