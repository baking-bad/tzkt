using Microsoft.AspNetCore.Mvc;
using Mvkt.Api.Models;
using Mvkt.Api.Repositories;

namespace Mvkt.Api.Controllers
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
