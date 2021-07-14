using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class AppStateCache
    {
        static AppState AppState = null;

        readonly TzktContext Db;

        public AppStateCache(TzktContext db)
        {
            Db = db;
        }

        public async Task ResetAsync()
        {
            AppState = await Db.AppState.SingleAsync();
        }

        public AppState Get()
        {
            return AppState;
        }

        public int GetLevel()
        {
            return AppState.Level;
        }

        public int GetNextLevel()
        {
            return AppState.Level + 1;
        }

        public string GetHead()
        {
            return AppState.Hash;
        }

        public string GetNextProtocol()
        {
            return AppState.NextProtocol;
        }

        public int NextAccountId()
        {
            Db.TryAttach(AppState);
            return ++AppState.AccountCounter;
        }

        public int NextOperationId()
        {
            Db.TryAttach(AppState);
            return ++AppState.OperationCounter;
        }

        public int NextBigMapId()
        {
            Db.TryAttach(AppState);
            return ++AppState.BigMapCounter;
        }

        public int NextBigMapKeyId()
        {
            Db.TryAttach(AppState);
            return ++AppState.BigMapKeyCounter;
        }

        public int NextBigMapUpdateId()
        {
            Db.TryAttach(AppState);
            return ++AppState.BigMapUpdateCounter;
        }

        public int NextStorageId()
        {
            Db.TryAttach(AppState);
            return ++AppState.StorageCounter;
        }

        public int NextScriptId()
        {
            Db.TryAttach(AppState);
            return ++AppState.ScriptCounter;
        }

        public int GetManagerCounter()
        {
            return AppState.ManagerCounter;
        }

        public void IncreaseManagerCounter(int value)
        {
            Db.TryAttach(AppState);
            AppState.ManagerCounter += value;
        }

        public void ReleaseManagerCounter()
        {
            Db.TryAttach(AppState);
            --AppState.ManagerCounter;
        }
    }
}
