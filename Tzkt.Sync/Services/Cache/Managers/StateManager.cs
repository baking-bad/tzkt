using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services
{
    public class StateManager
    {
        #region cache
        static AppState AppState = null;
        static Block CurrentBlock = null;
        static Block PreviousBlock = null;
        #endregion

        readonly TzktContext Db;

        public StateManager(TzktContext db)
        {
            Db = db;
        }

        public async Task<AppState> GetAppStateAsync()
        {
            AppState ??= await Db.AppState.FirstOrDefaultAsync()
                ?? throw new Exception("Failed to get app state");

            return AppState;
        }

        public async Task SetAppStateAsync(Block block, string nextProtocol)
        {
            var state = await GetAppStateAsync();

            if (block?.Level != state.Level + 1 || String.IsNullOrEmpty(nextProtocol))
                throw new Exception("Failed to set app state");

            PreviousBlock = CurrentBlock;
            CurrentBlock = block;

            state.Level = block.Level;
            state.Timestamp = block.Timestamp;
            state.Protocol = block.Protocol.Hash;
            state.NextProtocol = nextProtocol;
            state.Hash = block.Hash;
            
            Db.Update(state);
        }

        public async Task ReduceAppStateAsync()
        {
            var appState = await GetAppStateAsync();
            var currBlock = await GetCurrentBlock()
                ?? throw new Exception("Failed to reduce initial app state");
            var prevBlock = await GetPreviousBlock();

            PreviousBlock = null;
            CurrentBlock = prevBlock;

            appState.Level = prevBlock?.Level ?? -1;
            appState.Timestamp = prevBlock?.Timestamp ?? DateTime.MinValue;
            appState.Protocol = prevBlock?.Protocol.Hash ?? "";
            appState.NextProtocol = prevBlock == null ? "" : currBlock.Protocol.Hash;
            appState.Hash = prevBlock?.Hash ?? "";

            Db.Update(appState);
        }

        public async Task<int> GetCounter()
        {
            var state = await GetAppStateAsync();
            return state.Counter;
        }

        public async Task UpdateCounter(int change)
        {
            var state = await GetAppStateAsync();
            state.Counter += change;
        }

        public async Task<Block> GetCurrentBlock()
        {
            var state = await GetAppStateAsync();

            CurrentBlock ??= await Db.Blocks
                .Include(x => x.Protocol)
                .Include(x => x.Baker)
                .FirstOrDefaultAsync(x => x.Level == state.Level);

            return CurrentBlock;
        }

        public async Task<Block> GetPreviousBlock()
        {
            var state = await GetAppStateAsync();

            PreviousBlock ??= await Db.Blocks
                .Include(x => x.Protocol)
                .Include(x => x.Baker)
                .FirstOrDefaultAsync(x => x.Level == state.Level - 1);

            return PreviousBlock;
        }

        public void Clear()
        {
            AppState = null;
            CurrentBlock = null;
            PreviousBlock = null;
        }
    }
}
