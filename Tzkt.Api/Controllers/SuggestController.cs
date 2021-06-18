using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Tzkt.Api.Services.Metadata;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/suggest")]
    public class SuggestController : ControllerBase
    {
        private readonly AccountMetadataService AccountMetadata;

        public SuggestController(AccountMetadataService accounts)
        {
            AccountMetadata = accounts;
        }

        [OpenApiIgnore]
        [HttpGet("accounts")]
        public IEnumerable<AccountMetadataAlias> GetAccounts()
        {
            return AccountMetadata.Aliases;
        }

        /// <summary>
        /// Suggest accounts
        /// </summary>
        /// <remarks>
        /// Suggests known accounts by part of alias. This endpoint is useful for autocomplete.
        /// </remarks>
        /// <param name="search">Part of the account alias to search by</param>
        /// <returns></returns>
        [HttpGet("accounts/{search}")]
        public IEnumerable<AccountMetadataAlias> GetAccounts([Required] string search)
        {
            search = search.ToLower();
            var res = new List<(AccountMetadataAlias, int)>();

            foreach (var item in AccountMetadata.Aliases)
            {
                var alias = item.Alias.ToLower();

                if (alias == search)
                    res.Add((item, 0));
                else if (alias.StartsWith(search))
                    res.Add((item, 1));
                else if (alias.Contains(search))
                    res.Add((item, 2));
            }

            return res.OrderBy(x => x.Item2).ThenBy(x => x.Item1.Alias).Select(x => x.Item1).Take(10);
        }

        //[OpenApiIgnore]
        //[HttpGet("proposals")]
        //public IEnumerable<Services.Metadata.ProposalAlias> GetProposals()
        //{
        //    return ProposalMetadata.Aliases;
        //}

        ///// <summary>
        ///// Suggest proposals
        ///// </summary>
        ///// <remarks>
        ///// Suggests known proposals by part of alias. This endpoint is useful for autocomplete.
        ///// </remarks>
        ///// <param name="search">Part of the proposal alias to search by</param>
        ///// <returns></returns>
        //[HttpGet("proposals/{search}")]
        //public IEnumerable<Services.Metadata.ProposalAlias> GetProposals([Required] string search)
        //{
        //    search = search.ToLower();
        //    return ProposalMetadata.Aliases.Where(x => x.Alias.ToLower().Contains(search)).Take(10);
        //}

        //[OpenApiIgnore]
        //[HttpGet("protocols")]
        //public IEnumerable<ProtocolMetadataAlias> GetProtocols()
        //{
        //    return ProtocolMetadata.Aliases;
        //}

        ///// <summary>
        ///// Suggest protocols
        ///// </summary>
        ///// <remarks>
        ///// Suggests known protocols by part of alias. This endpoint is useful for autocomplete.
        ///// </remarks>
        ///// <param name="search">Part of the protocol alias to search by</param>
        ///// <returns></returns>
        //[HttpGet("protocols/{search}")]
        //public IEnumerable<ProtocolMetadataAlias> GetProtocols([Required] string search)
        //{
        //    search = search.ToLower();
        //    return ProtocolMetadata.Aliases.Where(x => x.Alias.ToLower().Contains(search)).Take(10);
        //}
    }
}
