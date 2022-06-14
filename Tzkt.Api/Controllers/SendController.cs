using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Services;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/send")]
    public class SendController : ControllerBase
    {
        readonly TezRpc Rpc;

        public SendController(TezRpc rpc)
        {
            Rpc = rpc;
        }

        [HttpGet("{signedTx}")]
        public async Task<ActionResult<string>> Send([Required] string signedTx, bool force = false)
        {
            try
            {
                return Ok(await Rpc.Send(signedTx));
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(signedTx), ex.Message);
            }
        }
    }
}
