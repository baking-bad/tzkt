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

        #region Software
        [HttpPost("software")]
        public async Task<ActionResult> UpdateSoftwareMetadata([FromHeader] AuthHeaders headers)
        {
            try
            {
                var jsonString = await Request.Body.GetStringAsync();

                if (!Auth.Authorized(headers, jsonString, out var error))
                    return Unauthorized(error);
                
                return Ok(await MetadataRepository.Update("Software", "ShortHash", JsonSerializer.Deserialize<List<Meta>>(jsonString)));
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
        #endregion

        #region Protocols
        [HttpPost("protocols")]
        public async Task<ActionResult> UpdateProtocolMetadata([FromHeader] AuthHeaders headers)
        {
            try
            {
                var jsonString = await Request.Body.GetStringAsync();
                
                if (!Auth.Authorized(headers, jsonString, out var error))
                    return Unauthorized(error);

                var metadata = JsonSerializer.Deserialize<List<Meta>>(jsonString);

                if (!metadata.All(x => Regex.IsMatch(x.Key, "^P[0-9A-z]{50}$")))
                    return BadRequest("Invalid protocol hash");
                
                return Ok(await MetadataRepository.Update("Protocols", "Hash", metadata));
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(headers), ex.Message);
            }
        }

        [HttpGet("protocols")]
        public async Task<ActionResult> GetProtocolsMetadata([FromHeader] AuthHeaders headers, OffsetParameter offset, [Range(0, 10000)] int limit = 100)
        {
            if (!Auth.Authorized(headers, out var error))
                return Unauthorized(error);
            return Ok(await MetadataRepository.GetMetadata("Protocols", "Hash", limit, offset));
        }
        #endregion

        #region Proposals
        [HttpPost("proposals")]
        public async Task<ActionResult> UpdateProposalMetadata([FromHeader] AuthHeaders headers)
        {
            try
            {
                var jsonString = await Request.Body.GetStringAsync();
                
                if (!Auth.Authorized(headers, jsonString, out var error))
                    return Unauthorized(error);

                var metadata = JsonSerializer.Deserialize<List<Meta>>(jsonString);

                if (!metadata.All(x => Regex.IsMatch(x.Key, "^P[0-9A-z]{50}$")))
                    return BadRequest("Invalid proposal hash");
                
                return Ok(await MetadataRepository.Update("Proposals", "Hash", metadata));
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
        #endregion

        #region Accounts
        [HttpPost("accounts")]
        public async Task<ActionResult> UpdateAccounts([FromHeader] AuthHeaders headers)
        {
            try
            {
                var jsonString = await Request.Body.GetStringAsync();
                
                if (!Auth.Authorized(headers, jsonString, out var error))
                    return Unauthorized(error);

                var metadata = JsonSerializer.Deserialize<List<Meta>>(jsonString);

                if (!metadata.All(x => Regex.IsMatch(x.Key, "^(tz1|tz2|tz3|KT1)[0-9A-z]{33}$")))
                    return BadRequest("Invalid address");
                
                return Ok(await MetadataRepository.Update("Accounts", "Address", metadata));
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
            return Ok(await MetadataRepository.GetAccounts(alias, limit, offset));
        }
        #endregion
    }
}