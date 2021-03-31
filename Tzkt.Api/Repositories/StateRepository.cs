using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

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

        public State Get()
        {
            var appState = State.Current;
            return new State
            {
                KnownLevel = appState.KnownHead,
                LastSync = appState.LastSync,
                Hash = appState.Hash,
                Level = appState.Level,
                Protocol = appState.Protocol,
                Timestamp = appState.Timestamp,
                VotingEpoch = appState.VotingEpoch,
                VotingPeriod = appState.VotingPeriod,
                QuoteLevel = appState.QuoteLevel,
                QuoteBtc = appState.QuoteBtc,
                QuoteEur = appState.QuoteEur,
                QuoteUsd = appState.QuoteUsd,
                QuoteCny = appState.QuoteCny,
                QuoteJpy = appState.QuoteJpy,
                QuoteKrw = appState.QuoteKrw,
                QuoteEth = appState.QuoteEth
            };
        }
    }
}
