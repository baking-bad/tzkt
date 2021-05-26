using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Authentication;
using Tzkt.Api.Repositories;
using TzKT_Client;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/metadata")]
    public class MetadataController : ControllerBase
    {
        private readonly MetadataRepository MetadataRepository;
        private readonly AuthService Auth;

        public MetadataController(MetadataRepository metadataRepository, AuthService auth)
        {
            MetadataRepository = metadataRepository;
            Auth = auth;
        }
                
        [HttpPost("software/update")]
        public async Task<ActionResult> UpdateSoftwareMetadata([FromBody] List<Met> value)
        {
            try
            {
                if (!Auth.Authorized(Request.Headers, value, out var error))
                    return Unauthorized(error);
                await MetadataRepository.Update("Software", "ShortHash", value);
                return Ok();
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(value), ex.Message);
            }
        }
    }
}