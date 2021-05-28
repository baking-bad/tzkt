using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
                var jsonString = await Request.Body.GetStringAsync();

                if (!Auth.Authorized(headers, jsonString, out var error))
                    return Unauthorized(error);
                
                await MetadataRepository.Update("Software", "ShortHash", JsonSerializer.Deserialize<List<Met>>(jsonString));
                //TODO Should we return the updated data?
                return Ok();
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(headers), ex.Message);
            }
        }

        [HttpGet("software")]
        public async Task<ActionResult> GetSoftwareMetadata([FromHeader] AuthHeaders headers, OffsetParameter offset, [Range(0, 10000)] int limit = 100)
        {
            //TODO Value filter like storage contracts
            if (!Auth.Authorized(headers, out var error))
                return Unauthorized(error);
            return Ok(await MetadataRepository.GetMetadata("Software", "ShortHash",0, 0));
        }

        [HttpPost("protocols/update")]
        public async Task<ActionResult> UpdateProtocolMetadata([FromHeader] AuthHeaders headers)
        {
            try
            {
                var jsonString = await Request.Body.GetStringAsync();
                
                if (!Auth.Authorized(headers, jsonString, out var error))
                    return Unauthorized(error);

                var value = JsonSerializer.Deserialize<List<Met>>(jsonString);

                if (!value.All(x => Regex.IsMatch(x.Key, "^P[0-9A-z]{50}$")))
                    return BadRequest("Invalid protocol hash");
                
                await MetadataRepository.Update("Protocols", "Hash", value);
                
                return Ok();
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(headers), ex.Message);
            }
        }
    }
}