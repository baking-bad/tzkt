using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/head")]
    public class HeadController : ControllerBase
    {
        private readonly StateRepository State;
        public HeadController(StateRepository state)
        {
            State = state;
        }

        /// <summary>
        /// Get indexer head
        /// </summary>
        /// <remarks>
        /// Returns indexer head and synchronization status.
        /// </remarks>
        /// <returns></returns>
        [HttpGet]
        public State Get()
        {
            return State.Get();
        }
    }
}
