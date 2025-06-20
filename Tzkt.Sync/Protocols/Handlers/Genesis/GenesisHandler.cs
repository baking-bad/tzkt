﻿using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using App.Metrics;
using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;
using Tzkt.Sync.Protocols.Genesis;

namespace Tzkt.Sync.Protocols
{
    class GenesisHandler : ProtocolHandler
    {
        public override IDiagnostics Diagnostics { get; }
        public override IValidator Validator { get; }
        public override IRpc Rpc { get; }
        public override string VersionName => "genesis";
        public override int VersionNumber => -1;

        public GenesisHandler(TezosNode node, TzktContext db, CacheService cache, QuotesService quotes, IServiceProvider services, IConfiguration config, ILogger<GenesisHandler> logger, IMetrics metrics)
            : base(node, db, cache, quotes, services, config, logger, metrics)
        {
            Diagnostics = new Diagnostics();
            Validator = new Validator(this);
            Rpc = new Rpc(node);
        }

        public override Task WarmUpCache(JsonElement block) => Task.CompletedTask;

        public override Task Commit(JsonElement rawBlock)
        {
            #region add protocol
            var protocol = new Protocol
            {
                Id = 0,
                Hash = rawBlock.RequiredString("protocol"),
                Code = -1,
                Version = VersionNumber,
                FirstLevel = 0,
                LastLevel = 0,
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
                Cycle = -1,
                Level = rawBlock.Required("header").RequiredInt32("level"),
                ProtoCode = protocol.Code,
                Timestamp = rawBlock.Required("header").RequiredDateTime("timestamp"),
                Events = BlockEvents.ProtocolBegin | BlockEvents.ProtocolEnd
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
            state.ChainId = rawBlock.RequiredString("chain_id");
            state.Chain = Chains.GetName(state.ChainId);
            state.Cycle = -1;
            state.Level = block.Level;
            state.Timestamp = block.Timestamp;
            state.Protocol = protocol.Hash;
            state.NextProtocol = rawBlock.Required("metadata").RequiredString("next_protocol");
            state.Hash = block.Hash;
            state.BlocksCount++;
            state.ProtocolsCount++;
            #endregion

            return Task.CompletedTask;
        }

        public override async Task Revert()
        {
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "Statistics";
                DELETE FROM "Protocols";
                DELETE FROM "Blocks";
                """);

            await Cache.Statistics.ResetAsync();
            await Cache.Protocols.ResetAsync();
            Cache.Blocks.Reset();

            #region update state
            var state = Cache.AppState.Get();
            state.ChainId = string.Empty;
            state.Chain = string.Empty;
            state.Cycle = -1;
            state.Level = -1;
            state.Timestamp = DateTimeOffset.MinValue.UtcDateTime;
            state.Protocol = string.Empty;
            state.NextProtocol = string.Empty;
            state.Hash = string.Empty;
            state.BlocksCount--;
            state.ProtocolsCount--;

            Cache.AppState.ReleaseOperationId();
            #endregion
        }
    }
}
