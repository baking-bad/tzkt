using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/domains")]
    public class DomainsController : ControllerBase
    {
        readonly DomainsRepository Repo;
        readonly ResponseCacheService Cache;

        public DomainsController(DomainsRepository repo, ResponseCacheService cache)
        {
            Repo = repo;
            Cache = cache;
        }

        /// <summary>
        /// Get domains count
        /// </summary>
        /// <remarks>
        /// Returns the total number of domains.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <returns></returns>
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetCount([FromQuery] DomainFilter filter)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value, ("filter", filter));

            if (Cache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Repo.GetCount(filter);
            cached = Cache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get domains
        /// </summary>
        /// <remarks>
        /// Returns a list of domains.
        /// </remarks>
        /// <param name="filter">Filter</param>
        /// <param name="pagination">Pagination</param>
        /// <param name="selection">Selection</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Domain>>> Get(
            [FromQuery] DomainFilter filter,
            [FromQuery] Pagination pagination,
            [FromQuery] Selection selection)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("filter", filter), ("pagination", pagination), ("selection", selection));

            if (Cache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (selection.select == null)
            {
                res = await Repo.Get(filter, pagination);
            }
            else
            {
                res = new SelectionResponse
                {
                    Cols = selection.select.Fields?.Select(x => x.Alias).ToArray(),
                    Rows = await Repo.Get(filter, pagination, selection.select.Fields ?? selection.select.Values)
                };
            }
            cached = Cache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get domain by name
        /// </summary>
        /// <remarks>
        /// Returns the domain with the specified name.
        /// </remarks>
        /// <param name="name">Domain name</param>
        /// <returns></returns>
        [HttpGet("{name}")]
        public async Task<ActionResult<Domain>> GetByName(string name)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (Cache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Repo.Get(name);
            cached = Cache.Set(query, res);
            return this.Bytes(cached);
        }
    }
}
