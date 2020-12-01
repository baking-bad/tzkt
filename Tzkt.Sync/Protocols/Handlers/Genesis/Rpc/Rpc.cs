﻿using System;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Genesis
{
    class Rpc : IRpc
    {
        readonly TezosNode Node;

        public Rpc(TezosNode node) => Node = node;

        #region indexer
        public Task<JsonElement> GetBlockAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}");

        public Task<JsonElement> GetCycleAsync(int level, int cycle)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetBakingRightsAsync(int level, int cycle)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetEndorsingRightsAsync(int level, int cycle)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetLevelBakingRightsAsync(int level, int maxPriority)
            => throw new InvalidOperationException();
        #endregion

        #region bootstrap
        public Task<JsonElement> GetAllContractsAsync(int level)
            => throw new InvalidOperationException();
        #endregion

        #region diagnostics
        public Task<JsonElement> GetGlobalCounterAsync(int level)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetContractAsync(int level, string address)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetDelegateAsync(int level, string address)
            => throw new InvalidOperationException();
        #endregion
    }
}
