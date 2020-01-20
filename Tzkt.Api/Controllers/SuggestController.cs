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
    [Route("v1/[controller]")]
    public class SuggestController : ControllerBase
    {
        private readonly AccountMetadataService AccountMetadata;

        public SuggestController(AccountMetadataService accountMetadata)
        {
            AccountMetadata = accountMetadata;
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
            return AccountMetadata.Aliases.Where(x => x.Alias.ToLower().Contains(search));
        }
    }
}
