using System.ComponentModel.DataAnnotations;
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

        /// <summary>
        /// Run script view
        /// </summary>
        /// <remarks>
        /// Simulate a call to a michelson view
        /// </remarks>
        /// <param name="contract">Contract address</param>
        /// <param name="view">Called view</param>
        /// <param name="input">Input(micheline michelson expression)/param>
        /// <returns></returns>
        [HttpGet("runScriptView")]
        public async Task<ActionResult> PostRunScriptView(            
            [Required][KTAddress] string contract,
            [Required] string view,
            [Required] string input)
        {
            try
            {
                return Ok(await Rpc.RunScriptView(contract, view, input));
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(contract), ex.Message);
            }
        }

        /// <summary>
        /// Run script view
        /// </summary>
        /// <remarks>
        /// Simulate a call to a michelson view
        /// </remarks>
        /// <param name="contract">Contract address</param>
        /// <param name="view">Called view</param>
        /// <param name="input">Input(micheline michelson expression)/param>
        /// <returns></returns>
        [HttpPost("runScriptView")]
        public async Task<ActionResult> PostRunScriptView(            
            [Required][KTAddress] string contract,
            [Required] string view,
            [FromBody] object input)
        {
            try
            {
                return Ok(await Rpc.RunScriptView(contract, view, input));
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(contract), ex.Message);
            }
        }
    }
}
