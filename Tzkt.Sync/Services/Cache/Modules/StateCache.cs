using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services
{
    public class StateCache
    {
        #region cache
        static AppState AppState = null;
        static Block CurrentBlock = null;
        static Block PreviousBlock = null;
        #endregion

        readonly TzktContext Db;

        public StateCache(TzktContext db)
        {
            Db = db;
        }

        public async Task<AppState> GetAppStateAsync()
        {
            AppState ??= await Db.AppState.FirstOrDefaultAsync()
                ?? throw new Exception("Failed to get app state");

            return AppState;
        }

        public async Task SetAppStateAsync(Block block)
        {
            var state = await GetAppStateAsync();

            PreviousBlock = block?.Level == state.Level + 1 ? CurrentBlock : null;
            CurrentBlock = block;

            state.Level = block?.Level ?? -1;
            state.Timestamp = block?.Timestamp ?? DateTime.MinValue;
            state.Protocol = block?.Protocol.Hash ?? "";
            state.Hash = block?.Hash ?? "";
            
            Db.Update(state);
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
