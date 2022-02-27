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

        #region state
        [HttpGet("state")]
        public async Task<ActionResult<RawJson>> GetStateMetadata(
            [FromHeader] AuthHeaders headers,
            string section = null)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetStateMetadata(section));
        }

        [HttpPost("state")]
        public async Task<ActionResult<MetadataUpdate>> UpdateStateMetadata(
            [FromHeader] AuthHeaders headers)
        {
            try
            {
                var body = await Request.Body.ReadAsStringAsync();

                if (!Auth.TryAuthenticate(headers, body, out var error))
                    return Unauthorized(error);

                var metadata = JsonSerializer.Deserialize<MetadataUpdate>(body);
                return Ok(await Metadata.UpdateStateMetadata(metadata));
            }
            catch (JsonException)
            {
                return new BadRequest("body", "Invalid json");
            }
        }
        #endregion

        #region accounts
        [HttpGet("accounts/{address}")]
        public async Task<ActionResult<RawJson>> GetAccountMetadata(
            [FromHeader] AuthHeaders headers,
            [Address] string address,
            string section = null)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetAccountMetadata(address, section));
        }

        [HttpGet("accounts")]
        public async Task<ActionResult<IEnumerable<MetadataUpdate<string>>>> GetAccountMetadata(
            [FromHeader] AuthHeaders headers,
            JsonParameter metadata,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100,
            string section = null)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetAccountMetadata(metadata, offset, limit, section));
        }

        [HttpPost("accounts")]
        public async Task<ActionResult<IEnumerable<MetadataUpdate<string>>>> UpdateAccountMetadata(
            [FromHeader] AuthHeaders headers)
        {
            try
            {
                var body = await Request.Body.ReadAsStringAsync();

                if (!Auth.TryAuthenticate(headers, body, out var error))
                    return Unauthorized(error);

                var metadata = JsonSerializer.Deserialize<List<MetadataUpdate<string>>>(body);
                if (metadata.Any(x => !Regex.IsMatch(x.Key, "^(tz1|tz2|tz3|KT1)[0-9A-Za-z]{33}$")))
                    return new BadRequest("body", "Invalid account address");

                return Ok(await Metadata.UpdateAccountMetadata(metadata));
            }
            catch (JsonException)
            {
                return new BadRequest("body", "Invalid json");
            }
        }
        #endregion

        #region proposals
        [HttpGet("proposals/{hash}")]
        public async Task<ActionResult<RawJson>> GetProposalMetadata(
            [FromHeader] AuthHeaders headers,
            [ProtocolHash] string hash,
            string section = null)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetProposalMetadata(hash, section));
        }

        [HttpGet("proposals")]
        public async Task<ActionResult<IEnumerable<MetadataUpdate<string>>>> GetProposalMetadata(
            [FromHeader] AuthHeaders headers,
            JsonParameter metadata,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100,
            string section = null)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetProposalMetadata(metadata, offset, limit, section));
        }

        [HttpPost("proposals")]
        public async Task<ActionResult<IEnumerable<MetadataUpdate<string>>>> UpdateProposalMetadata(
            [FromHeader] AuthHeaders headers)
        {
            try
            {
                var body = await Request.Body.ReadAsStringAsync();

                if (!Auth.TryAuthenticate(headers, body, out var error))
                    return Unauthorized(error);

                var metadata = JsonSerializer.Deserialize<List<MetadataUpdate<string>>>(body);
                if (metadata.Any(x => !Regex.IsMatch(x.Key, "^P[0-9A-Za-z]{50}$")))
                    return BadRequest("Invalid proposal hash");

                return Ok(await Metadata.UpdatProposalMetadata(metadata));
            }
            catch (JsonException)
            {
                return new BadRequest("body", "Invalid json");
            }
        }
        #endregion

        #region protocols
        [HttpGet("protocols/{hash}")]
        public async Task<ActionResult<RawJson>> GetProtocolMetadata(
            [FromHeader] AuthHeaders headers,
            [ProtocolHash] string hash,
            string section = null)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetProtocolMetadata(hash, section));
        }

        [HttpGet("protocols")]
        public async Task<ActionResult<IEnumerable<MetadataUpdate<string>>>> GetProtocolMetadata(
            [FromHeader] AuthHeaders headers,
            JsonParameter metadata,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100,
            string section = null)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetProtocolMetadata(metadata, offset, limit, section));
        }

        [HttpPost("protocols")]
        public async Task<ActionResult<IEnumerable<MetadataUpdate<string>>>> UpdateProtocolMetadata(
            [FromHeader] AuthHeaders headers)
        {
            try
            {
                var body = await Request.Body.ReadAsStringAsync();

                if (!Auth.TryAuthenticate(headers, body, out var error))
                    return Unauthorized(error);

                var metadata = JsonSerializer.Deserialize<List<MetadataUpdate<string>>>(body);
                if (metadata.Any(x => !Regex.IsMatch(x.Key, "^P[0-9A-Za-z]{50}$")))
                    return BadRequest("Invalid protocol hash");

                return Ok(await Metadata.UpdatProtocolMetadata(metadata));
            }
            catch (JsonException)
            {
                return new BadRequest("body", "Invalid json");
            }
        }
        #endregion

        #region software
        [HttpGet("software/{shortHash}")]
        public async Task<ActionResult<RawJson>> GetSoftwareMetadata(
            [FromHeader] AuthHeaders headers,
            [Hex(8)] string shortHash,
            string section = null)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetSoftwareMetadata(shortHash, section));
        }

        [HttpGet("software")]
        public async Task<ActionResult<IEnumerable<MetadataUpdate<string>>>> GetSoftwareMetadata(
            [FromHeader] AuthHeaders headers,
            JsonParameter metadata,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100,
            string section = null)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetSoftwareMetadata(metadata, offset, limit, section));
        }

        [HttpPost("software")]
        public async Task<ActionResult<IEnumerable<MetadataUpdate<string>>>> UpdateSoftwareMetadata(
            [FromHeader] AuthHeaders headers)
        {
            try
            {
                var body = await Request.Body.ReadAsStringAsync();
                if (!Auth.TryAuthenticate(headers, body, out var error))
                    return Unauthorized(error);

                var metadata = JsonSerializer.Deserialize<List<MetadataUpdate<string>>>(body);
                if (metadata.Any(x => !Regex.IsMatch(x.Key, "^[0-9a-f]{8}$")))
                    return BadRequest("Invalid software short hash");

                return Ok(await Metadata.UpdateSoftwareMetadata(metadata));
            }
            catch (JsonException)
            {
                return new BadRequest("body", "Invalid json");
            }
        }
        #endregion

        #region constants
        [HttpGet("constants/{address}")]
        public async Task<ActionResult<RawJson>> GetConstantMetadata(
            [FromHeader] AuthHeaders headers,
            [ExpressionHash] string address,
            string section = null)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetConstantMetadata(address, section));
        }

        [HttpGet("constants")]
        public async Task<ActionResult<IEnumerable<MetadataUpdate<string>>>> GetConstantMetadata(
            [FromHeader] AuthHeaders headers,
            JsonParameter metadata,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100,
            string section = null)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetConstantMetadata(metadata, offset, limit, section));
        }

        [HttpPost("constants")]
        public async Task<ActionResult<IEnumerable<MetadataUpdate<string>>>> UpdateConstantMetadata(
            [FromHeader] AuthHeaders headers)
        {
            try
            {
                var body = await Request.Body.ReadAsStringAsync();
                if (!Auth.TryAuthenticate(headers, body, out var error))
                    return Unauthorized(error);

                var metadata = JsonSerializer.Deserialize<List<MetadataUpdate<string>>>(body);
                if (metadata.Any(x => !Regex.IsMatch(x.Key, "^expr[0-9A-Za-z]{50}$")))
                    return BadRequest("Invalid expression hash");

                return Ok(await Metadata.UpdateConstantMetadata(metadata));
            }
            catch (JsonException)
            {
                return new BadRequest("body", "Invalid json");
            }
        }
        #endregion

        #region blocks
        [HttpGet("blocks/{level:int}")]
        public async Task<ActionResult<RawJson>> GetBlockMetadata(
            [FromHeader] AuthHeaders headers,
            [Min(0)] int level,
            string section = null)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetBlockMetadata(level, section));
        }

        [HttpGet("blocks")]
        public async Task<ActionResult<IEnumerable<MetadataUpdate<int>>>> GetBlockMetadata(
            [FromHeader] AuthHeaders headers,
            JsonParameter metadata,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100,
            string section = null)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetBlockMetadata(metadata, offset, limit, section));
        }

        [HttpPost("blocks")]
        public async Task<ActionResult<IEnumerable<MetadataUpdate<int>>>> UpdateBlockMetadata(
            [FromHeader] AuthHeaders headers)
        {
            try
            {
                var body = await Request.Body.ReadAsStringAsync();
                if (!Auth.TryAuthenticate(headers, body, out var error))
                    return Unauthorized(error);

                var metadata = JsonSerializer.Deserialize<List<MetadataUpdate<int>>>(body);
                return Ok(await Metadata.UpdateBlockMetadata(metadata));
            }
            catch (JsonException)
            {
                return new BadRequest("body", "Invalid json");
            }
        }
        #endregion

        #region tokens
        [HttpGet("tokens/{id:int}")]
        public async Task<ActionResult<RawJson>> GetTokenMetadata(
            [FromHeader] AuthHeaders headers,
            [Min(0)] int id,
            string section = null)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetTokenMetadata(id, section));
        }

        [HttpGet("tokens")]
        public async Task<ActionResult<IEnumerable<MetadataUpdate<int>>>> GetTokenMetadata(
            [FromHeader] AuthHeaders headers,
            JsonParameter metadata,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100,
            string section = null)
        {
            if (!Auth.TryAuthenticate(headers, out var error))
                return Unauthorized(error);

            return Ok(await Metadata.GetTokenMetadata(metadata, offset, limit, section));
        }

        [HttpPost("tokens")]
        public async Task<ActionResult<IEnumerable<MetadataUpdate<int>>>> UpdateTokenMetadata(
            [FromHeader] AuthHeaders headers)
        {
            try
            {
                var body = await Request.Body.ReadAsStringAsync();
                if (!Auth.TryAuthenticate(headers, body, out var error))
                    return Unauthorized(error);

                var metadata = JsonSerializer.Deserialize<List<MetadataUpdate<int>>>(body);
                return Ok(await Metadata.UpdateTokenMetadata(metadata));
            }
            catch (JsonException)
            {
                return new BadRequest("body", "Invalid json");
            }
        }
        #endregion
    }
}