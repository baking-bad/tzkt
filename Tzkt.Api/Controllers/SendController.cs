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
        readonly NodeRpc Rpc;

        public SendController(NodeRpc rpc)
        {
            Rpc = rpc;
        }

        /// <summary>
        /// Inject an operation in node and broadcast it.
        /// </summary>
        /// <remarks>
        /// Returns the operation hash.
        /// </remarks>
        /// <param name="signedTx">Signed operation contents, constructed and signed by the client</param>
        /// <param name="force"> If parameter is true, the function returns immediately. Otherwise, the operation will be validated before the result is returned.</param>
        /// <returns></returns>
        [HttpGet("{signedTx}")]
        public async Task<ActionResult<string>> Send([Required] string signedTx, bool force = false)
        {
            try
            {
                return Ok(await Rpc.Send(signedTx, force));
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(signedTx), ex.Message);
            }
        }
        
        /// <summary>
        /// Inject an operation in node and broadcast it.
        /// </summary>
        /// <remarks>
        /// Returns the operation hash.
        /// </remarks>
        /// <param name="signedTx">Signed operation contents, constructed and signed by the client</param>
        /// <param name="force"> If parameter is true, the function returns immediately. Otherwise, the operation will be validated before the result is returned.</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<string>> Post([FromBody][Required] string signedTx, bool force = false)
        {
            if (string.IsNullOrEmpty(signedTx))
            {
                return new BadRequest($"{nameof(signedTx)}", "Body is null");

            }
            
            try
            {
                return Ok(await Rpc.Send(signedTx, force));
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(signedTx), ex.Message);
            }
        }
    }
}
