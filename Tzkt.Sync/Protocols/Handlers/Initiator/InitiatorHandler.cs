﻿using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using App.Metrics;
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
        public override string VersionName => "genesis";
        public override int VersionNumber => 0;

        public InitiatorHandler(TezosNode node, TzktContext db, CacheService cache, QuotesService quotes, IServiceProvider services, IConfiguration config, ILogger<InitiatorHandler> logger, IMetrics metrics)
            : base(node, db, cache, quotes, services, config, logger, metrics)
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
                Id = 0,
                Hash = rawBlock.RequiredString("protocol"),
                Code = 0,
                Version = VersionNumber,
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
                ProtoCode = protocol.Code,
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
            var stats = new Statistics
            {
                Id = 0,
                Level = block.Level
            };
            Db.Statistics.Add(stats);
            Cache.Statistics.SetCurrent(stats);
            #endregion

            #region update state
            var state = Cache.AppState.Get();
            state.Cycle = 0;
            state.Level = block.Level;
            state.Timestamp = block.Timestamp;
            state.Protocol = protocol.Hash;
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
            var currProtocol = await Cache.Protocols.GetAsync(curr.ProtoCode);

            var prev = await Cache.Blocks.PreviousAsync();
            var prevProtocol = await Cache.Protocols.GetAsync(prev.ProtoCode);

            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "Statistics" WHERE "Level" = {0};
                DELETE FROM "Protocols" WHERE "FirstLevel" = {0};
                DELETE FROM "Blocks" WHERE "Level" = {0};
                """, curr.Level);

            await Cache.Statistics.ResetAsync();
            await Cache.Protocols.ResetAsync();
            Cache.Blocks.Reset();

            #region update state
            var state = Cache.AppState.Get();
            state.Cycle = -1;
            state.Level = prev.Level;
            state.Timestamp = prev.Timestamp;
            state.Protocol = prevProtocol.Hash;
            state.NextProtocol = currProtocol.Hash;
            state.Hash = prev.Hash;
            state.BlocksCount--;
            state.ProtocolsCount--;

            Cache.AppState.ReleaseOperationId();
            #endregion
        }
    }
}
