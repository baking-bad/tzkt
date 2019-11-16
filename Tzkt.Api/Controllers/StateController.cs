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
    [Route("api/[controller]")]
    public class StateController : ControllerBase
    {
        private readonly StateRepository State;
        public StateController(StateRepository state)
        {
            State = state;
        }

        [HttpGet]
        public Task<State> Get()
        {
            return State.Get();
        }
    }
}
