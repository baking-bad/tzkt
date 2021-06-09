using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
                
                return Ok(await MetadataRepository.Update("Software", "ShortHash", JsonSerializer.Deserialize<List<Met>>(jsonString)));
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(headers), ex.Message);
            }
        }

        [HttpGet("software")]
        public async Task<ActionResult> GetSoftwareMetadata([FromHeader] AuthHeaders headers, OffsetParameter offset, [Range(0, 10000)] int limit = 100)
        {
            if (!Auth.Authorized(headers, out var error))
                return Unauthorized(error);
            
            return Ok(await MetadataRepository.GetMetadata("Software", "ShortHash", limit, offset));
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
                
                return Ok(await MetadataRepository.Update("Protocols", "Hash", value));
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(headers), ex.Message);
            }
        }

        [HttpGet("protocols")]
        public async Task<ActionResult> GetProtocolsMetadata([FromHeader] AuthHeaders headers, OffsetParameter offset, [Range(0, 10000)] int limit = 100)
        {
            //TODO Value filter like storage contracts
            if (!Auth.Authorized(headers, out var error))
                return Unauthorized(error);
            return Ok(await MetadataRepository.GetMetadata("Protocols", "Hash", limit, offset));
        }

        [HttpPost("proposals/update")]
        public async Task<ActionResult> UpdateProposalMetadata([FromHeader] AuthHeaders headers)
        {
            try
            {
                var jsonString = await Request.Body.GetStringAsync();
                
                if (!Auth.Authorized(headers, jsonString, out var error))
                    return Unauthorized(error);

                var value = JsonSerializer.Deserialize<List<Met>>(jsonString);

                if (!value.All(x => Regex.IsMatch(x.Key, "^P[0-9A-z]{50}$")))
                    return BadRequest("Invalid proposal hash");
                
                return Ok(await MetadataRepository.Update("Proposals", "Hash", value));
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(headers), ex.Message);
            }
        }

        [HttpGet("proposals")]
        public async Task<ActionResult> GetProposalsMetadata([FromHeader] AuthHeaders headers, OffsetParameter offset, [Range(0, 10000)] int limit = 100)
        {
            if (!Auth.Authorized(headers, out var error))
                return Unauthorized(error);
            return Ok(await MetadataRepository.GetMetadata("Proposals", "Hash", limit, offset));
        }

        [HttpPost("accounts/update")]
        public async Task<ActionResult> UpdateAccountsMetadata([FromHeader] AuthHeaders headers)
        {
            try
            {
                var jsonString = await Request.Body.GetStringAsync();
                
                if (!Auth.Authorized(headers, jsonString, out var error))
                    return Unauthorized(error);

                var value = JsonSerializer.Deserialize<List<Met>>(jsonString);

                if (!value.All(x => Regex.IsMatch(x.Key, "^(tz1|tz2|tz3|KT1)[0-9A-z]{33}$")))
                    return BadRequest("Invalid address");
                
                return Ok(await MetadataRepository.Update("Accounts", "Address", value));
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(headers), ex.Message);
            }
        }

        [HttpGet("accounts")]
        public async Task<ActionResult> GetAccountsMetadata([FromHeader] AuthHeaders headers, string alias, OffsetParameter offset, [Range(0, 10000)] int limit = 100)
        {
            if (!Auth.Authorized(headers, out var error))
                return Unauthorized(error);
            return Ok(await MetadataRepository.GetFilteredMetadata(alias, limit, offset));
        }
        
        
    }
}