﻿using Microsoft.EntityFrameworkCore;
using Mvkt.Data;
using Mvkt.Data.Models;
using Mvkt.Data.Models.Base;

namespace Mvkt.Sync.Services.Cache
{
    public class AppStateCache
    {
        static AppState AppState = null;

        readonly MvktContext Db;

        public AppStateCache(MvktContext db)
        {
            Db = db;
        }

        public void UpdateSyncState(int knownHead, DateTime lastSync)
        {
            if (AppState != null)
            {
                AppState.KnownHead = knownHead;
                AppState.LastSync = lastSync;
            }
        }

        public async Task ResetAsync()
        {
            AppState = await Db.AppState.SingleAsync();
        }

        public AppState Get()
        {
            return AppState;
        }

        public string GetChainId()
        {
            return AppState.ChainId;
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
            return ++AppState.AccountCounter;
        }

        public void ReleaseAccountId(int count = 1)
        {
            AppState.AccountCounter -= count;
        }

        public long NextOperationId()
        {
            return (++AppState.OperationCounter << AppState.SubIdBits);
        }

        public void ReleaseOperationId(int count = 1)
        {
            AppState.OperationCounter -= count;
        }

        public int NextBigMapId()
        {
            return ++AppState.BigMapCounter;
        }

        public void ReleaseBigMapId(int count = 1)
        {
            AppState.BigMapCounter -= count;
        }

        public int NextBigMapKeyId()
        {
            return ++AppState.BigMapKeyCounter;
        }

        public void ReleaseBigMapKeyId(int count = 1)
        {
            AppState.BigMapKeyCounter -= count;
        }

        public int NextBigMapUpdateId()
        {
            return ++AppState.BigMapUpdateCounter;
        }

        public void ReleaseBigMapUpdateId(int count = 1)
        {
            AppState.BigMapUpdateCounter -= count;
        }

        public int NextEventId()
        {
            return ++AppState.EventCounter;
        }

        public void ReleaseEventId(int count)
        {
            AppState.EventCounter -= count;
        }

        public int NextSmartRollupCommitmentId()
        {
            return ++AppState.SmartRollupCommitmentCounter;
        }

        public void ReleaseSmartRollupCommitmentId()
        {
            AppState.SmartRollupCommitmentCounter--;
        }

        public int NextRefutationGameId()
        {
            return ++AppState.RefutationGameCounter;
        }

        public void ReleaseRefutationGameId()
        {
            AppState.RefutationGameCounter--;
        }

        public int NextInboxMessageId()
        {
            return ++AppState.InboxMessageCounter;
        }

        public void ReleaseInboxMessageId(int count)
        {
            AppState.InboxMessageCounter -= count;
        }

        public int NextStorageId()
        {
            return ++AppState.StorageCounter;
        }

        public void ReleaseStorageId(int count = 1)
        {
            AppState.StorageCounter -= count;
        }

        public int NextScriptId()
        {
            return ++AppState.ScriptCounter;
        }

        public void ReleaseScriptId(int count = 1)
        {
            AppState.ScriptCounter -= count;
        }

        public long NextSubId(ContractOperation op)
        {
            op.SubIds = (op.SubIds ?? 0) +  1;
            if (op.SubIds >= 1 << AppState.SubIdBits) throw new Exception("SubId overflow");
            return op.Id + (int)op.SubIds;
        }

        public long NextSubId(TransferTicketOperation op)
        {
            op.SubIds = (op.SubIds ?? 0) +  1;
            if (op.SubIds >= 1 << AppState.SubIdBits) throw new Exception("SubId overflow");
            return op.Id + (int)op.SubIds;
        }

        public long NextSubId(SmartRollupExecuteOperation op)
        {
            op.SubIds = (op.SubIds ?? 0) +  1;
            if (op.SubIds >= 1 << AppState.SubIdBits) throw new Exception("SubId overflow");
            return op.Id + (int)op.SubIds;
        }

        public long NextSubId(MigrationOperation op)
        {
            op.SubIds = (op.SubIds ?? 0) + 1;
            if (op.SubIds >= 1 << AppState.SubIdBits) throw new Exception("SubId overflow");
            return op.Id + (int)op.SubIds;
        }

        public int GetManagerCounter()
        {
            return AppState.ManagerCounter;
        }

        public void IncreaseManagerCounter(int value)
        {
            AppState.ManagerCounter += value;
        }

        public void ReleaseManagerCounter()
        {
            --AppState.ManagerCounter;
        }
    }
}
