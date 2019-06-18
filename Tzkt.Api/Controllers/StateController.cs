using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StateController : ControllerBase
    {
        private readonly ApiContext Db;
        public StateController(ApiContext db)
        {
            Db = db;
        }

        [HttpGet]
        public async Task<ActionResult<object>> Get()
        {
            var state = await Db.AppState.FirstOrDefaultAsync();
            return new 
            {
                hash = state.Hash,
                level = state.Level,
                timestamp = state.Timestamp,
            };
        }
    }
}
