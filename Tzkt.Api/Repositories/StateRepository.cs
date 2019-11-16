using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services;

namespace Tzkt.Api.Repositories
{
    public class StateRepository : DbConnection
    {
        readonly StateService StateService;

        public StateRepository(StateService stateService, IConfiguration config) : base(config)
        {
            StateService = stateService;
        }

        public async Task<State> Get()
        {
            var appState = await StateService.GetState();
            
            return new State
            {
                Hash = appState.Hash,
                Level = appState.Level,
                Timestamp = appState.Timestamp
            };
        }
    }
}
