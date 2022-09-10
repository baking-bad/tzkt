using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;
using Tzkt.Sync.Protocols.Initiator;

namespace Tzkt.Sync.Protocols
{
    class InitiatorHandler : ProtocolHandler
    {
        public override IDiagnostics Diagnostics { get; }
        public override IValidator Validator { get; }
        public override IRpc Rpc { get; }

        public InitiatorHandler(TezosNode node, TzktContext db, CacheService cache, QuotesService quotes, IServiceProvider services, IConfiguration config, ILogger<InitiatorHandler> logger)
            : base(node, db, cache, quotes, services, config, logger)
        {
            Diagnostics = new Diagnostics();
            Validator = new Validator(this);
            Rpc = new Rpc(node);
        }

        public override Task Activate(AppState state, JsonElement block) => Task.CompletedTask;
        public override Task Deactivate(AppState state) => Task.CompletedTask;

        public override Task WarmUpCache(JsonElement block) => Task.CompletedTask;

        public override Task Commit(JsonElement rawBlock)
        {
            #region add protocol
            var protocol = new Protocol
            {
                Hash = rawBlock.RequiredString("protocol"),
                Code = 0,
                FirstLevel = 1,
                LastLevel = 1,
                FirstCycle = 0,
                FirstCycleLevel = 1
            };
            Db.Protocols.Add(protocol);
            Cache.Protocols.Add(protocol);
            #endregion

            #region add block
            var block = new Block
            {
                Id = Cache.AppState.NextOperationId(),
                Hash = rawBlock.RequiredString("hash"),
                Cycle = 0,
                Level = rawBlock.Required("header").RequiredInt32("level"),
                Protocol = protocol,
                Timestamp = rawBlock.Required("header").RequiredDateTime("timestamp"),
                Events = BlockEvents.CycleBegin
                    | BlockEvents.ProtocolBegin
                    | BlockEvents.ProtocolEnd
                    | BlockEvents.BalanceSnapshot
            };
            Db.Blocks.Add(block);
            Cache.Blocks.Add(block);
            #endregion

            #region add empty stats
            var stats = new Statistics { Level = block.Level };
            Db.Statistics.Add(stats);
            Cache.Statistics.Add(stats);
            #endregion

            #region update state
            var state = Cache.AppState.Get();
            state.Cycle = 0;
            state.Level = block.Level;
            state.Timestamp = block.Timestamp;
            state.Protocol = block.Protocol.Hash;
            state.NextProtocol = rawBlock.Required("metadata").RequiredString("next_protocol");
            state.Hash = block.Hash;
            state.BlocksCount++;
            state.ProtocolsCount++;
            state.VotingEpoch = 0;
            state.VotingPeriod = 0;
            #endregion

            return Task.CompletedTask;
        }

        public override async Task Revert()
        {
            var curr = Cache.Blocks.Current();
            curr.Protocol ??= await Cache.Protocols.GetAsync(curr.ProtoCode);

            var prev = await Cache.Blocks.PreviousAsync();
            prev.Protocol ??= await Cache.Protocols.GetAsync(prev.ProtoCode);

            await Db.Database.ExecuteSqlRawAsync($@"
                DELETE FROM ""Statistics"" WHERE ""Level"" = {curr.Level};
                DELETE FROM ""Protocols"" WHERE ""FirstLevel"" = {curr.Level};
                DELETE FROM ""Blocks"" WHERE ""Level"" = {curr.Level};");

            Cache.Statistics.Reset();
            Cache.Protocols.Reset();
            Cache.Blocks.Reset();

            #region update state
            var state = Cache.AppState.Get();
            state.Cycle = -1;
            state.Level = prev.Level;
            state.Timestamp = prev.Timestamp;
            state.Protocol = prev.Protocol.Hash;
            state.NextProtocol = curr.Protocol.Hash;
            state.Hash = prev.Hash;
            state.BlocksCount--;
            state.ProtocolsCount--;

            Cache.AppState.ReleaseOperationId();
            #endregion
        }
    }
}
