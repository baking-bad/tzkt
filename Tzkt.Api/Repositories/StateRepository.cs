using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class StateRepository : DbConnection
    {
        readonly StateCache State;

        public StateRepository(StateCache state, IConfiguration config) : base(config)
        {
            State = state;
        }

        public Task<State> Get()
        {
            var appState = State.GetState();
            
            return Task.FromResult(new State
            {
                Hash = appState.Hash,
                Level = appState.Level,
                Protocol = appState.Protocol,
                Timestamp = appState.Timestamp
            });
        }
    }
}
