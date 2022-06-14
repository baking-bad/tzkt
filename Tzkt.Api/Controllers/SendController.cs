using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Models;
using Tzkt.Api.Services;
using Tzkt.Api.Services.Auth;

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

            //TODO Handle broken body
            /*if (string.IsNullOrEmpty(body))
            {
                return new BadRequest($"{nameof(@body)}", "Body is null");

            }*/
            try
            {
                return Ok(await Rpc.Send(signedTx));
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(signedTx), ex.Message);
            }
            
        }

        [HttpPost]
        public async Task<ActionResult<string>> Post(bool force = false)
        {
            var body = await Request.Body.ReadAsStringAsync();

            //TODO Handle broken body
            /*if (string.IsNullOrEmpty(body))
            {
                return new BadRequest($"{nameof(@body)}", "Body is null");

            }*/
            return Ok(await Rpc.Send(body));
        }
    }
}
