using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Repositories;

namespace Tzkt.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("v1/metadata")]
    public class MetdataController : ControllerBase
    {
        private readonly SoftwareRepository Software;

        public MetdataController(SoftwareRepository software)
        {
            Software = software;
        }
                
        [HttpPost("update/{software}")]
        public async Task<ActionResult> UpdateHash(string software, [FromBody] object value)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await Software.Update(software, json);
                Console.WriteLine("Posted Software");
                return Ok();
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(value), ex.Message);
            }
        }
    }
}