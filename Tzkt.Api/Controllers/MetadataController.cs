using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

using Tzkt.Api.Services.Auth;
using Tzkt.Api.Repositories;

namespace Tzkt.Api.Controllers
{
    [OpenApiIgnore]
    [ApiController]
    [Route("v1/metadata")]
    public class MetadataController : ControllerBase
    {
        readonly MetadataRepository Metadata;
        readonly IAuthService Auth;

        public MetadataController(MetadataRepository metadata, IAuthService auth)
        {
            Metadata = metadata;
            Auth = auth;
        }

        #region accounts
        [HttpGet("accounts")]
        public async Task<ActionResult<IEnumerable<ObjectMetadata>>> GetAccountMetadata(
            [FromHeader] AuthHeaders headers,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);
            
            return Ok(await Metadata.GetAccountMetadata(offset, limit));
        }

        [HttpGet("accounts/{address}")]
        public async Task<ActionResult<RawJson>> GetAccountMetadata(
            [FromHeader] AuthHeaders headers,
            [Address] string address)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetAccountMetadata(address));
        }

        [HttpPost("accounts")]
        public async Task<ActionResult<IEnumerable<ObjectMetadata>>> UpdateAccountMetadata(
            [FromHeader] AuthHeaders headers)
        {
            try
            {
                var body = await Request.Body.ReadAsStringAsync();

                if (!Auth.TryAuthenticate(headers, body, out var error))
                    return Unauthorized(error);

                var metadata = JsonSerializer.Deserialize<List<ObjectMetadata>>(body);
                if (metadata.Any(x => !Regex.IsMatch(x.Key, "^(tz1|tz2|tz3|KT1)[0-9A-Za-z]{33}$")))
                    return BadRequest("Invalid account address");

                return Ok(await Metadata.UpdateAccountMetadata(metadata));
            }
            catch (JsonException ex)
            {
                return new BadRequest("body", ex.Message);
            }
        }
        #endregion

        #region proposals
        [HttpGet("proposals")]
        public async Task<ActionResult<IEnumerable<ObjectMetadata>>> GetProposalMetadata(
            [FromHeader] AuthHeaders headers,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetProposalMetadata(offset, limit));
        }

        [HttpGet("proposals/{hash}")]
        public async Task<ActionResult<RawJson>> GetProposalMetadata(
            [FromHeader] AuthHeaders headers,
            [ProtocolHash] string hash)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetProposalMetadata(hash));
        }

        [HttpPost("proposals")]
        public async Task<ActionResult<IEnumerable<ObjectMetadata>>> UpdateProposalMetadata([FromHeader] AuthHeaders headers)
        {
            try
            {
                var body = await Request.Body.ReadAsStringAsync();

                if (!Auth.TryAuthenticate(headers, body, out var error))
                    return Unauthorized(error);

                var metadata = JsonSerializer.Deserialize<List<ObjectMetadata>>(body);
                if (metadata.Any(x => !Regex.IsMatch(x.Key, "^P[0-9A-Za-z]{50}$")))
                    return BadRequest("Invalid proposal hash");

                return Ok(await Metadata.UpdatProposalMetadata(metadata));
            }
            catch (JsonException ex)
            {
                return new BadRequest("body", ex.Message);
            }
        }
        #endregion

        #region protocols
        [HttpGet("protocols")]
        public async Task<ActionResult<IEnumerable<ObjectMetadata>>> GetProtocolMetadata(
            [FromHeader] AuthHeaders headers,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetProtocolMetadata(offset, limit));
        }

        [HttpGet("protocols/{hash}")]
        public async Task<ActionResult<RawJson>> GetProtocolMetadata(
            [FromHeader] AuthHeaders headers,
            [ProtocolHash] string hash)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetProtocolMetadata(hash));
        }

        [HttpPost("protocols")]
        public async Task<ActionResult<IEnumerable<ObjectMetadata>>> UpdateProtocolMetadata([FromHeader] AuthHeaders headers)
        {
            try
            {
                var body = await Request.Body.ReadAsStringAsync();

                if (!Auth.TryAuthenticate(headers, body, out var error))
                    return Unauthorized(error);

                var metadata = JsonSerializer.Deserialize<List<ObjectMetadata>>(body);
                if (metadata.Any(x => !Regex.IsMatch(x.Key, "^P[0-9A-Za-z]{50}$")))
                    return BadRequest("Invalid protocol hash");

                return Ok(await Metadata.UpdatProtocolMetadata(metadata));
            }
            catch (JsonException ex)
            {
                return new BadRequest("body", ex.Message);
            }
        }
        #endregion

        #region software
        [HttpGet("software")]
        public async Task<ActionResult<IEnumerable<ObjectMetadata>>> GetSoftwareMetadata(
            [FromHeader] AuthHeaders headers,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetSoftwareMetadata(offset, limit));
        }

        [HttpGet("software/{shortHash}")]
        public async Task<ActionResult<RawJson>> GetSoftwareMetadata(
            [FromHeader] AuthHeaders headers,
            string shortHash)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetSoftwareMetadata(shortHash));
        }

        [HttpPost("software")]
        public async Task<ActionResult<IEnumerable<ObjectMetadata>>> UpdateSoftwareMetadata([FromHeader] AuthHeaders headers)
        {
            try
            {
                var body = await Request.Body.ReadAsStringAsync();
                if (!Auth.TryAuthenticate(headers, body, out var error))
                    return Unauthorized(error);

                var metadata = JsonSerializer.Deserialize<List<ObjectMetadata>>(body);
                if (metadata.Any(x => !Regex.IsMatch(x.Key, "^[0-9a-f]{8}$")))
                    return BadRequest("Invalid software short hash");

                return Ok(await Metadata.UpdateSoftwareMetadata(metadata));
            }
            catch (JsonException ex)
            {
                return new BadRequest("body", ex.Message);
            }
        }
        #endregion
    }
}