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

            state.Level = block?.Level ?? -1;
            state.Timestamp = block?.Timestamp ?? DateTime.MinValue;
            state.Protocol = block?.Protocol.Hash ?? "";
            state.Hash = block?.Hash ?? "";

            Db.Update(state);
        }

        public void Clear() => AppState = null;
    }
}
