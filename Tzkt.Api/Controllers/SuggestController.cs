using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Metadata;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/suggest")]
    public class SuggestController : ControllerBase
    {
        private readonly AccountMetadataService AccountMetadata;
        private readonly ProposalMetadataService ProposalMetadata;
        private readonly ProtocolMetadataService ProtocolMetadata;

        public SuggestController(AccountMetadataService accounts, ProposalMetadataService proposals, ProtocolMetadataService protocols)
        {
            AccountMetadata = accounts;
            ProposalMetadata = proposals;
            ProtocolMetadata = protocols;
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
        public IEnumerable<AccountMetadataAlias> GetAccounts(string search)
        {
            search = search.ToLower();
            return AccountMetadata.Aliases.Where(x => x.Alias.ToLower().Contains(search)).Take(10);
        }

        [OpenApiIgnore]
        [HttpGet("proposals")]
        public IEnumerable<ProposalMetadataAlias> GetProposals()
        {
            return ProposalMetadata.Aliases;
        }

        /// <summary>
        /// Suggest proposals
        /// </summary>
        /// <remarks>
        /// Suggests known proposals by part of alias. This endpoint is useful for autocomplete.
        /// </remarks>
        /// <param name="search">Part of the proposal alias to search by</param>
        /// <returns></returns>
        [HttpGet("proposals/{search}")]
        public IEnumerable<ProposalMetadataAlias> GetProposals(string search)
        {
            search = search.ToLower();
            return ProposalMetadata.Aliases.Where(x => x.Alias.ToLower().Contains(search)).Take(10);
        }

        [OpenApiIgnore]
        [HttpGet("protocols")]
        public IEnumerable<ProtocolMetadataAlias> GetProtocols()
        {
            return ProtocolMetadata.Aliases;
        }

        /// <summary>
        /// Suggest protocols
        /// </summary>
        /// <remarks>
        /// Suggests known protocols by part of alias. This endpoint is useful for autocomplete.
        /// </remarks>
        /// <param name="search">Part of the protocol alias to search by</param>
        /// <returns></returns>
        [HttpGet("protocols/{search}")]
        public IEnumerable<ProtocolMetadataAlias> GetProtocols(string search)
        {
            search = search.ToLower();
            return ProtocolMetadata.Aliases.Where(x => x.Alias.ToLower().Contains(search)).Take(10);
        }
    }
}
