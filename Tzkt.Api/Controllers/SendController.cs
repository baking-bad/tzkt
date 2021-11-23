using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
