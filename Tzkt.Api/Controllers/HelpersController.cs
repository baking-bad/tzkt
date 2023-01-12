using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Services;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/helpers")]
    public class HelpersController : ControllerBase
    {
        readonly RpcHelpers Rpc;

        public HelpersController(RpcHelpers rpc)
        {
            Rpc = rpc;
        }

        /// <summary>
        /// Inject operation
        /// </summary>
        /// <remarks>
        /// Injects signed operation to the blockchain and returns the operation hash.
        /// </remarks>
        /// <param name="bytes">Forged operation bytes + signature in hex format.</param>
        /// <param name="async">If `true`, the function returns immediately. Otherwise, the operation will be validated before the result is returned.</param>
        /// <returns></returns>
        [HttpPost("inject")]
        public async Task<ActionResult<string>> PostInject([FromBody][Required] string bytes, bool async = true)
        {
            try
            {
                return Ok(await Rpc.Inject(bytes, async));
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(bytes), ex.Message);
            }
        }
    }
}
