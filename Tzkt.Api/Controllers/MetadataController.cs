using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Repositories;
using TzKT_Client;

namespace Tzkt.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("v1/metadata")]
    public class MetadataController : ControllerBase
    {
        private readonly MetadataRepository MetadataRepository;

        public MetadataController(MetadataRepository metadataRepository)
        {
            MetadataRepository = metadataRepository;
        }
                
        [HttpPost("software/update")]
        public async Task<ActionResult> UpdateSoftwareMetadata([FromBody] List<Met> value)
        {
            try
            {
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