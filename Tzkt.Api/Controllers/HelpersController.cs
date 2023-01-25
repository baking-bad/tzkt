﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Netezos.Contracts;
using Netezos.Encoding;

using Tzkt.Api.Repositories;
using Tzkt.Api.Services;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/helpers")]
    public class HelpersController : ControllerBase
    {
        readonly RpcHelpers Rpc;
        readonly AccountRepository Accounts;

        public HelpersController(RpcHelpers rpc,  AccountRepository accounts)
        {
            Rpc = rpc;
            Accounts = accounts;
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
        /// <param name="name">Called view</param>
        /// <param name="input">Input(micheline michelson expression)/param>
        /// <returns></returns>
        [HttpPost("view/{contract}/{name}")]
        public async Task<ActionResult> PostRunScriptView(            
            [Required][KTAddress] string contract,
            [Required] string name,
            [FromBody] object input)
        {
            try
            {
                var schema = await Accounts.GetViewSchema(contract, name);
                if (schema == null)
                    return Ok(null);

                var inputType = Schema.Create(schema.Args[1] as MichelinePrim);
                var res = await Rpc.RunScriptView(contract, name, inputType.Optimize(inputType.MapObject(input, true)));
                return Ok(Schema.Create(schema.Args[2] as MichelinePrim).Humanize(res));
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(contract), ex.Message);
            }
        }
    }
}
