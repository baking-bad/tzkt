using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Authentication;
using Tzkt.Api.Repositories;

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
        public async Task<ActionResult> UpdateSoftwareMetadata([FromHeader] AuthHeaders headers)
        {
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var jsonString = await reader.ReadToEndAsync();
                var value = JsonSerializer.Deserialize<List<Met>>(jsonString);

                if (!Auth.Authorized(headers, jsonString, out var error))
                    return Unauthorized(error);
                await MetadataRepository.Update("Software", "ShortHash", value);
                //TODO Should we return the updated data?
                return Ok();
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(headers), ex.Message);
            }
        }

        [HttpGet("software")]
        public async Task<ActionResult> GetSoftwareMetadata( [FromHeader] AuthHeaders headers)
        {
            //TODO Value filter like storage contracts
            if (!Auth.Authorized(headers, null, out var error))
                return Unauthorized(error);
            return Ok(await MetadataRepository.GetMetadata("Software", "ShortHash",0, 0));
        }

        [HttpPost("protocols/update")]
        public async Task<ActionResult> UpdateProtocolMetadata([FromHeader] AuthHeaders headers)
        {
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var jsonString = await reader.ReadToEndAsync();
                var value = JsonSerializer.Deserialize<List<Met>>(jsonString);
                if (!Auth.Authorized(headers, jsonString, out var error))
                    return Unauthorized(error);
                await MetadataRepository.Update("Protocols", "Hash", value);
                //TODO Should we return the updated data?
                return Ok();
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(headers), ex.Message);
            }
        }
    }
}