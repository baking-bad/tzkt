using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Tzkt.Api.Services.Auth;
using Tzkt.Api.Repositories;

namespace Tzkt.Api.Controllers
{
    [OpenApiIgnore]
    [ApiController]
    [Route("v1/extras")]
    [Route("v1/metadata")] // [DEPRECATED]
    public class ExtrasController : ControllerBase
    {
        readonly ExtrasRepository Extras;
        readonly IAuthService Auth;

        public ExtrasController(ExtrasRepository extras, IAuthService auth)
        {
            Extras = extras;
            Auth = auth;
        }

        #region state
        [HttpGet("state")]
        public async Task<ActionResult<RawJson>> GetStateExtras(
            [FromHeader] AuthHeaders headers,
            string section = null)
        {
            var rights = new AccessRights()
            {
                Table = "AppState",
                Section = section,
                Access = Access.Read
            };

            if (!Auth.TryAuthenticate(headers, rights, out var error))
                return Unauthorized(error);

            return Ok(await Extras.GetStateExtras(section));
        }

        [HttpPost("state")]
        public async Task<ActionResult<ExtrasUpdate>> UpdateStateExtras(
            [FromHeader] AuthHeaders headers,
            string section = null)
        {
            try
            {
                var rights = new AccessRights()
                {
                    Table = "AppState",
                    Section = section,
                    Access = Access.Write
                };

                var body = await Request.Body.ReadAsStringAsync();

                if (!Auth.TryAuthenticate(headers, rights, body, out var error))
                    return Unauthorized(error);

                var extras = JsonSerializer.Deserialize<ExtrasUpdate>(body);
                return Ok(await Extras.UpdateStateExtras(extras, section));
            }
            catch (JsonException)
            {
                return new BadRequest("body", "Invalid json");
            }
        }
        #endregion

        #region accounts
        [HttpGet("accounts/{address}")]
        public async Task<ActionResult<RawJson>> GetAccountExtras(
            [FromHeader] AuthHeaders headers,
            [Address] string address,
            string section = null)
        {
            var rights = new AccessRights()
            {
                Table = "Accounts",
                Section = section,
                Access = Access.Read
            };
            
            if (!Auth.TryAuthenticate(headers, rights, out var error))
                return Unauthorized(error);

            return Ok(await Extras.GetAccountExtras(address, section));
        }

        [HttpGet("accounts")]
        public async Task<ActionResult<IEnumerable<ExtrasUpdate<string>>>> GetAccountExtras(
            [FromHeader] AuthHeaders headers,
            JsonParameter extras,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100,
            string section = null)
        {
            var rights = new AccessRights()
            {
                Table = "Accounts",
                Section = section,
                Access = Access.Read
            };

            if (!Auth.TryAuthenticate(headers, rights, out var error))
                return Unauthorized(error);

            return Ok(await Extras.GetAccountExtras(extras, offset, limit, section));
        }

        [HttpPost("accounts")]
        public async Task<ActionResult<IEnumerable<ExtrasUpdate<string>>>> UpdateAccountExtras(
            [FromHeader] AuthHeaders headers,
            string section = null)
        {
            try
            {
                var rights = new AccessRights()
                {
                    Table = "Accounts",
                    Section = section,
                    Access = Access.Write
                };
                
                var body = await Request.Body.ReadAsStringAsync();

                if (!Auth.TryAuthenticate(headers, rights, body, out var error))
                    return Unauthorized(error);

                var extras = JsonSerializer.Deserialize<List<ExtrasUpdate<string>>>(body);
                if (extras.Any(x => !Regex.IsMatch(x.Key, "^(tz1|tz2|tz3|KT1|txr1)[0-9A-Za-z]{33}$")))
                    return new BadRequest("body", "Invalid account address");

                return Ok(await Extras.UpdateAccountExtras(extras, section));
            }
            catch (JsonException)
            {
                return new BadRequest("body", "Invalid json");
            }
        }
        #endregion

        #region proposals
        [HttpGet("proposals/{hash}")]
        public async Task<ActionResult<RawJson>> GetProposalExtras(
            [FromHeader] AuthHeaders headers,
            [ProtocolHash] string hash,
            string section = null)
        {
            var rights = new AccessRights()
            {
                Table = "Proposals",
                Section = section,
                Access = Access.Read
            };
            
            if (!Auth.TryAuthenticate(headers, rights, out var error))
                return Unauthorized(error);

            return Ok(await Extras.GetProposalExtras(hash, section));
        }

        [HttpGet("proposals")]
        public async Task<ActionResult<IEnumerable<ExtrasUpdate<string>>>> GetProposalExtras(
            [FromHeader] AuthHeaders headers,
            JsonParameter extras,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100,
            string section = null)
        {
            var rights = new AccessRights()
            {
                Table = "Proposals",
                Section = section,
                Access = Access.Read
            };
            
            if (!Auth.TryAuthenticate(headers, rights, out var error))
                return Unauthorized(error);

            return Ok(await Extras.GetProposalExtras(extras, offset, limit, section));
        }

        [HttpPost("proposals")]
        public async Task<ActionResult<IEnumerable<ExtrasUpdate<string>>>> UpdateProposalExtras(
            [FromHeader] AuthHeaders headers,
            string section = null)
        {
            try
            {
                var rights = new AccessRights()
                {
                    Table = "Proposals",
                    Section = section,
                    Access = Access.Write
                };
                
                var body = await Request.Body.ReadAsStringAsync();

                if (!Auth.TryAuthenticate(headers, rights, body, out var error))
                    return Unauthorized(error);

                var extras = JsonSerializer.Deserialize<List<ExtrasUpdate<string>>>(body);
                if (extras.Any(x => !Regex.IsMatch(x.Key, "^P[0-9A-Za-z]{50}$")))
                    return BadRequest("Invalid proposal hash");

                return Ok(await Extras.UpdateProposalExtras(extras, section));
            }
            catch (JsonException)
            {
                return new BadRequest("body", "Invalid json");
            }
        }
        #endregion

        #region protocols
        [HttpGet("protocols/{hash}")]
        public async Task<ActionResult<RawJson>> GetProtocolExtras(
            [FromHeader] AuthHeaders headers,
            [ProtocolHash] string hash,
            string section = null)
        {
            var rights = new AccessRights()
            {
                Table = "Protocols",
                Section = section,
                Access = Access.Read
            };
            
            if (!Auth.TryAuthenticate(headers, rights, out var error))
                return Unauthorized(error);

            return Ok(await Extras.GetProtocolExtras(hash, section));
        }

        [HttpGet("protocols")]
        public async Task<ActionResult<IEnumerable<ExtrasUpdate<string>>>> GetProtocolExtras(
            [FromHeader] AuthHeaders headers,
            JsonParameter extras,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100,
            string section = null)
        {
            var rights = new AccessRights()
            {
                Table = "Protocols",
                Section = section,
                Access = Access.Read
            };
            
            if (!Auth.TryAuthenticate(headers, rights, out var error))
                return Unauthorized(error);

            return Ok(await Extras.GetProtocolExtras(extras, offset, limit, section));
        }

        [HttpPost("protocols")]
        public async Task<ActionResult<IEnumerable<ExtrasUpdate<string>>>> UpdateProtocolExtras(
            [FromHeader] AuthHeaders headers,
            string section = null)
        {
            try
            {
                var rights = new AccessRights()
                {
                    Table = "Protocols",
                    Section = section,
                    Access = Access.Write
                };
                
                var body = await Request.Body.ReadAsStringAsync();

                if (!Auth.TryAuthenticate(headers, rights, body, out var error))
                    return Unauthorized(error);

                var extras = JsonSerializer.Deserialize<List<ExtrasUpdate<string>>>(body);
                if (extras.Any(x => !Regex.IsMatch(x.Key, "^P[0-9A-Za-z]{50}$")))
                    return BadRequest("Invalid protocol hash");

                return Ok(await Extras.UpdateProtocolExtras(extras, section));
            }
            catch (JsonException)
            {
                return new BadRequest("body", "Invalid json");
            }
        }
        #endregion

        #region software
        [HttpGet("software/{shortHash}")]
        public async Task<ActionResult<RawJson>> GetSoftwareExtras(
            [FromHeader] AuthHeaders headers,
            [Hex(8)] string shortHash,
            string section = null)
        {
            var rights = new AccessRights()
            {
                Table = "Software",
                Section = section,
                Access = Access.Read
            };
            
            if (!Auth.TryAuthenticate(headers, rights, out var error))
                return Unauthorized(error);

            return Ok(await Extras.GetSoftwareExtras(shortHash, section));
        }

        [HttpGet("software")]
        public async Task<ActionResult<IEnumerable<ExtrasUpdate<string>>>> GetSoftwareExtras(
            [FromHeader] AuthHeaders headers,
            JsonParameter extras,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100,
            string section = null)
        {
            var rights = new AccessRights()
            {
                Table = "Software",
                Section = section,
                Access = Access.Read
            };
            
            if (!Auth.TryAuthenticate(headers, rights, out var error))
                return Unauthorized(error);

            return Ok(await Extras.GetSoftwareExtras(extras, offset, limit, section));
        }

        [HttpPost("software")]
        public async Task<ActionResult<IEnumerable<ExtrasUpdate<string>>>> UpdateSoftwareExtras(
            [FromHeader] AuthHeaders headers,
            string section = null)
        {
            try
            {
                var rights = new AccessRights()
                {
                    Table = "Software",
                    Section = section,
                    Access = Access.Write
                };

                var body = await Request.Body.ReadAsStringAsync();
                if (!Auth.TryAuthenticate(headers, rights, body, out var error))
                    return Unauthorized(error);

                var extras = JsonSerializer.Deserialize<List<ExtrasUpdate<string>>>(body);
                if (extras.Any(x => !Regex.IsMatch(x.Key, "^[0-9a-f]{8}$")))
                    return BadRequest("Invalid software short hash");

                return Ok(await Extras.UpdateSoftwareExtras(extras, section));
            }
            catch (JsonException)
            {
                return new BadRequest("body", "Invalid json");
            }
        }
        #endregion

        #region constants
        [HttpGet("constants/{address}")]
        public async Task<ActionResult<RawJson>> GetConstantExtras(
            [FromHeader] AuthHeaders headers,
            [ExpressionHash] string address,
            string section = null)
        {
            var rights = new AccessRights()
            {
                Table = "Constants",
                Section = section,
                Access = Access.Read
            };

            if (!Auth.TryAuthenticate(headers, rights, out var error))
                return Unauthorized(error);

            return Ok(await Extras.GetConstantExtras(address, section));
        }

        [HttpGet("constants")]
        public async Task<ActionResult<IEnumerable<ExtrasUpdate<string>>>> GetConstantExtras(
            [FromHeader] AuthHeaders headers,
            JsonParameter extras,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100,
            string section = null)
        {
            var rights = new AccessRights()
            {
                Table = "Constants",
                Section = section,
                Access = Access.Read
            };
            
            if (!Auth.TryAuthenticate(headers, rights, out var error))
                return Unauthorized(error);

            return Ok(await Extras.GetConstantExtras(extras, offset, limit, section));
        }

        [HttpPost("constants")]
        public async Task<ActionResult<IEnumerable<ExtrasUpdate<string>>>> UpdateConstantExtras(
            [FromHeader] AuthHeaders headers,
            string section = null)
        {
            try
            {
                var rights = new AccessRights()
                {
                    Table = "Constants",
                    Section = section,
                    Access = Access.Write
                };
                
                var body = await Request.Body.ReadAsStringAsync();
                if (!Auth.TryAuthenticate(headers, rights,body, out var error))
                    return Unauthorized(error);

                var extras = JsonSerializer.Deserialize<List<ExtrasUpdate<string>>>(body);
                if (extras.Any(x => !Regex.IsMatch(x.Key, "^expr[0-9A-Za-z]{50}$")))
                    return BadRequest("Invalid expression hash");

                return Ok(await Extras.UpdateConstantExtras(extras, section));
            }
            catch (JsonException)
            {
                return new BadRequest("body", "Invalid json");
            }
        }
        #endregion

        #region blocks
        [HttpGet("blocks/{level:int}")]
        public async Task<ActionResult<RawJson>> GetBlockExtras(
            [FromHeader] AuthHeaders headers,
            [Min(0)] int level,
            string section = null)
        {
            var rights = new AccessRights()
            {
                Table = "Blocks",
                Section = section,
                Access = Access.Read
            };
            
            if (!Auth.TryAuthenticate(headers, rights, out var error))
                return Unauthorized(error);

            return Ok(await Extras.GetBlockExtras(level, section));
        }

        [HttpGet("blocks")]
        public async Task<ActionResult<IEnumerable<ExtrasUpdate<int>>>> GetBlockExtras(
            [FromHeader] AuthHeaders headers,
            JsonParameter extras,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100,
            string section = null)
        {
            var rights = new AccessRights()
            {
                Table = "Blocks",
                Section = section,
                Access = Access.Read
            };
            
            if (!Auth.TryAuthenticate(headers, rights, out var error))
                return Unauthorized(error);

            return Ok(await Extras.GetBlockExtras(extras, offset, limit, section));
        }

        [HttpPost("blocks")]
        public async Task<ActionResult<IEnumerable<ExtrasUpdate<int>>>> UpdateBlockExtras(
            [FromHeader] AuthHeaders headers,
            string section = null)
        {
            try
            {
                var rights = new AccessRights()
                {
                    Table = "Blocks",
                    Section = section,
                    Access = Access.Write
                };
                
                var body = await Request.Body.ReadAsStringAsync();
                if (!Auth.TryAuthenticate(headers, rights, body, out var error))
                    return Unauthorized(error);

                var extras = JsonSerializer.Deserialize<List<ExtrasUpdate<int>>>(body);
                return Ok(await Extras.UpdateBlockExtras(extras, section));
            }
            catch (JsonException)
            {
                return new BadRequest("body", "Invalid json");
            }
        }
        #endregion
    }
}