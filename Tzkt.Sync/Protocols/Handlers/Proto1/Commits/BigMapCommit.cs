using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;
using Netezos.Encoding;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto1
{
    class BigMapCommit : ProtocolCommit
    {
        readonly List<(ContractOperation op, Contract contract, BigMapDiff diff)> Diffs = new();
        readonly Dictionary<int, int> TempPtrs = new(7);
        int TempPtr = 0;

        public BigMapCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual void Append(ContractOperation op, Contract contract, IEnumerable<BigMapDiff> diffs)
        {
            foreach (var diff in diffs)
            {
                #region transform temp ptrs
                if (diff.Ptr < 0)
                {
                    if (diff.Action <= BigMapDiffAction.Copy)
                    {
                        TempPtrs[diff.Ptr] = --TempPtr;
                        diff.Ptr = TempPtr;
                    }
                    else
                    {
                        diff.Ptr = TempPtrs[diff.Ptr];
                    }
                }
                else if (diff is CopyDiff copy && copy.SourcePtr < 0)
                {
                    copy.SourcePtr = TempPtrs[copy.SourcePtr];
                }
                #endregion
                Diffs.Add((op, contract, diff));
            }
        }

        public virtual async Task Apply()
        {
            if (Diffs.Count == 0) return;
            Diffs[0].op.Block.Events |= BlockEvents.Bigmaps;

            #region prefetch
            var allocated = new HashSet<int>(7);
            var copiedFrom = new HashSet<int>(7);

            foreach (var diff in Diffs.Where(x => x.diff.Ptr >= 0))
            {
                if (diff.diff.Action == BigMapDiffAction.Alloc)
                {
                    allocated.Add(diff.diff.Ptr);
                }
                else if (diff.diff is CopyDiff copy)
                {
                    var origin = GetOrigin(copy);
                    if (origin < 0)
                        allocated.Add(diff.diff.Ptr);
                    else
                        copiedFrom.Add(origin);
                }
            }

            await Cache.BigMaps.Prefetch(Diffs
                .Where(x => x.diff.Ptr >= 0 && !allocated.Contains(x.diff.Ptr))
                .Select(x => x.diff.Ptr));

            await Cache.BigMapKeys.Prefetch(Diffs
                .Where(x => x.diff.Ptr >= 0 && !allocated.Contains(x.diff.Ptr) && x.diff.Action == BigMapDiffAction.Update)
                .Select(x => (x.diff.Ptr, (x.diff as UpdateDiff).KeyHash)));

            var copiedKeys = copiedFrom.Count == 0 ? null :
                await Db.BigMapKeys.AsNoTracking().Where(x => copiedFrom.Contains(x.BigMapPtr)).ToListAsync();
            #endregion

            var images = new Dictionary<int, Dictionary<string, BigMapKey>>();
            foreach (var diff in Diffs)
            {
                switch (diff.diff)
                {
                    case AllocDiff alloc:
                        if (alloc.Ptr >= 0)
                        {
                            #region allocate new
                            var script = await Cache.Schemas.GetAsync(diff.contract);
                            var storage = await Cache.Storages.GetAsync(diff.contract);
                            var storageView = script.Storage.Schema.ToTreeView(Micheline.FromBytes(storage.RawValue));
                            var bigMapNode = storageView.Nodes()
                                .FirstOrDefault(x => x.Schema.Prim == PrimType.big_map && x.Value is MichelineInt v && v.Value == alloc.Ptr);

                            if (bigMapNode == null)
                            {
                                storage = Db.ChangeTracker.Entries()
                                    .FirstOrDefault(x => x.Entity is Storage s && (s.OriginationId == diff.op.Id || s.TransactionId == diff.op.Id))
                                    .Entity as Storage;
                                storageView = script.Storage.Schema.ToTreeView(Micheline.FromBytes(storage.RawValue));
                                bigMapNode = storageView.Nodes()
                                    .FirstOrDefault(x => x.Schema.Prim == PrimType.big_map && x.Value is MichelineInt v && v.Value == alloc.Ptr)
                                        ?? throw new Exception($"Allocated big_map {alloc.Ptr} missed in the storage");
                            }

                            var bigMapSchema = bigMapNode.Schema as BigMapSchema;

                            Db.BigMapUpdates.Add(new BigMapUpdate
                            {
                                Id = Cache.AppState.NextBigMapUpdateId(),
                                Action = BigMapAction.Allocate,
                                BigMapPtr = alloc.Ptr,
                                Level = diff.op.Level,
                                TransactionId = (diff.op as TransactionOperation)?.Id,
                                OriginationId = (diff.op as OriginationOperation)?.Id
                            });
                            diff.op.BigMapUpdates = (diff.op.BigMapUpdates ?? 0) + 1;

                            var allocatedBigMap = new BigMap
                            {
                                Id = Cache.AppState.NextBigMapId(),
                                Ptr = alloc.Ptr,
                                ContractId = diff.contract.Id,
                                StoragePath = bigMapNode.Path,
                                KeyType = bigMapSchema.Key.ToMicheline().ToBytes(),
                                ValueType = bigMapSchema.Value.ToMicheline().ToBytes(),
                                Active = true,
                                FirstLevel = diff.op.Level,
                                LastLevel = diff.op.Level,
                                ActiveKeys = 0,
                                TotalKeys = 0,
                                Updates = 1,
                                Tags = BigMaps.GetTags(bigMapNode)
                            };
                            Db.BigMaps.Add(allocatedBigMap);
                            Cache.BigMaps.Cache(allocatedBigMap);

                            images.Add(alloc.Ptr, new());
                            #endregion
                        }
                        else
                        {
                            #region alloc temp
                            images.Add(alloc.Ptr, new());
                            #endregion
                        }
                        break;
                    case CopyDiff copy:
                        if (copy.SourcePtr >= 0 && !copiedFrom.Contains(copy.SourcePtr))
                            break;
                        if (!images.TryGetValue(copy.SourcePtr, out var src))
                        {
                            src = copiedKeys
                                .Where(x => x.BigMapPtr == copy.SourcePtr)
                                .ToDictionary(x => x.KeyHash);
                        }
                        if (copy.Ptr >= 0)
                        {
                            #region copy to new
                            var script = await Cache.Schemas.GetAsync(diff.contract);
                            var storage = await Cache.Storages.GetAsync(diff.contract);
                            var storageView = script.Storage.Schema.ToTreeView(Micheline.FromBytes(storage.RawValue));
                            var bigMapNode = storageView.Nodes()
                                .FirstOrDefault(x => x.Schema.Prim == PrimType.big_map && x.Value is MichelineInt v && v.Value == copy.Ptr);

                            if (bigMapNode == null)
                            {
                                storage = Db.ChangeTracker.Entries()
                                    .FirstOrDefault(x => x.Entity is Storage s && (s.OriginationId == diff.op.Id || s.TransactionId == diff.op.Id))
                                    .Entity as Storage;
                                storageView = script.Storage.Schema.ToTreeView(Micheline.FromBytes(storage.RawValue));
                                bigMapNode = storageView.Nodes()
                                    .FirstOrDefault(x => x.Schema.Prim == PrimType.big_map && x.Value is MichelineInt v && v.Value == copy.Ptr)
                                        ?? throw new Exception($"Copied big_map {copy.Ptr} missed in the storage");
                            }

                            var bigMapSchema = bigMapNode.Schema as BigMapSchema;

                            var keys = src.Values.Select(x =>
                            {
                                var rawKey = Micheline.FromBytes(x.RawKey);
                                var rawValue = Micheline.FromBytes(x.RawValue);
                                return new BigMapKey
                                {
                                    Id = Cache.AppState.NextBigMapKeyId(),
                                    BigMapPtr = copy.Ptr,
                                    Active = true,
                                    KeyHash = x.KeyHash,
                                    JsonKey = bigMapSchema.Key.Humanize(rawKey),
                                    JsonValue = bigMapSchema.Value.Humanize(rawValue),
                                    RawKey = bigMapSchema.Key.Optimize(rawKey).ToBytes(),
                                    RawValue = bigMapSchema.Value.Optimize(rawValue).ToBytes(),
                                    FirstLevel = diff.op.Level,
                                    LastLevel = diff.op.Level,
                                    Updates = 1
                                };
                            }).ToList();

                            Db.BigMapKeys.AddRange(keys);
                            Cache.BigMapKeys.Cache(keys);

                            Db.BigMapUpdates.Add(new BigMapUpdate
                            {
                                Id = Cache.AppState.NextBigMapUpdateId(),
                                Action = BigMapAction.Allocate,
                                BigMapPtr = copy.Ptr,
                                Level = diff.op.Level,
                                TransactionId = (diff.op as TransactionOperation)?.Id,
                                OriginationId = (diff.op as OriginationOperation)?.Id
                            });
                            Db.BigMapUpdates.AddRange(keys.Select(x => new BigMapUpdate
                            {
                                Id = Cache.AppState.NextBigMapUpdateId(),
                                Action = BigMapAction.AddKey,
                                BigMapKeyId = x.Id,
                                BigMapPtr = x.BigMapPtr,
                                JsonValue = x.JsonValue,
                                RawValue = x.RawValue,
                                Level = x.FirstLevel,
                                TransactionId = (diff.op as TransactionOperation)?.Id,
                                OriginationId = (diff.op as OriginationOperation)?.Id
                            }));
                            diff.op.BigMapUpdates = (diff.op.BigMapUpdates ?? 0) + keys.Count + 1;

                            var copiedBigMap = new BigMap
                            {
                                Id = Cache.AppState.NextBigMapId(),
                                Ptr = copy.Ptr,
                                ContractId = diff.contract.Id,
                                StoragePath = bigMapNode.Path,
                                KeyType = bigMapSchema.Key.ToMicheline().ToBytes(),
                                ValueType = bigMapSchema.Value.ToMicheline().ToBytes(),
                                Active = true,
                                FirstLevel = diff.op.Level,
                                LastLevel = diff.op.Level,
                                ActiveKeys = keys.Count,
                                TotalKeys = keys.Count,
                                Updates = keys.Count + 1,
                                Tags = BigMaps.GetTags(bigMapNode)
                            };

                            Db.BigMaps.Add(copiedBigMap);
                            Cache.BigMaps.Cache(copiedBigMap);

                            images.Add(copy.Ptr, keys.ToDictionary(x => x.KeyHash));
                            #endregion
                        }
                        else
                        {
                            #region copy to temp
                            images.Add(copy.Ptr, src.Values
                                .Select(x => new BigMapKey
                                {
                                    KeyHash = x.KeyHash,
                                    RawKey = x.RawKey,
                                    RawValue = x.RawValue
                                })
                                .ToDictionary(x => x.KeyHash));
                            #endregion
                        }
                        break;
                    case UpdateDiff update:
                        if (update.Ptr >= 0)
                        {
                            var bigMap = Cache.BigMaps.Get(update.Ptr);

                            if (Cache.BigMapKeys.TryGet(update.Ptr, update.KeyHash, out var key))
                            {
                                if (update.Value != null)
                                {
                                    #region update key
                                    Db.TryAttach(bigMap);
                                    bigMap.LastLevel = diff.op.Level;
                                    if (!key.Active) bigMap.ActiveKeys++;
                                    bigMap.Updates++;

                                    Db.TryAttach(key);
                                    key.Active = true;
                                    key.JsonValue = bigMap.Schema.Value.Humanize(update.Value);
                                    key.RawValue = bigMap.Schema.Value.Optimize(update.Value).ToBytes();
                                    key.LastLevel = diff.op.Level;
                                    key.Updates++;

                                    Db.BigMapUpdates.Add(new BigMapUpdate
                                    {
                                        Id = Cache.AppState.NextBigMapUpdateId(),
                                        Action = BigMapAction.UpdateKey,
                                        BigMapKeyId = key.Id,
                                        BigMapPtr = key.BigMapPtr,
                                        JsonValue = key.JsonValue,
                                        RawValue = key.RawValue,
                                        Level = key.LastLevel,
                                        TransactionId = (diff.op as TransactionOperation)?.Id,
                                        OriginationId = (diff.op as OriginationOperation)?.Id
                                    });
                                    diff.op.BigMapUpdates = (diff.op.BigMapUpdates ?? 0) + 1;
                                    #endregion
                                }
                                else if (key.Active) // WTF: edo2net:76611 - key was removed twice
                                {
                                    #region remove key
                                    Db.TryAttach(bigMap);
                                    bigMap.LastLevel = diff.op.Level;
                                    bigMap.ActiveKeys--;
                                    bigMap.Updates++;

                                    Db.TryAttach(key);
                                    key.Active = false;
                                    key.LastLevel = diff.op.Level;
                                    key.Updates++;

                                    Db.BigMapUpdates.Add(new BigMapUpdate
                                    {
                                        Id = Cache.AppState.NextBigMapUpdateId(),
                                        Action = BigMapAction.RemoveKey,
                                        BigMapKeyId = key.Id,
                                        BigMapPtr = key.BigMapPtr,
                                        JsonValue = key.JsonValue,
                                        RawValue = key.RawValue,
                                        Level = key.LastLevel,
                                        TransactionId = (diff.op as TransactionOperation)?.Id,
                                        OriginationId = (diff.op as OriginationOperation)?.Id
                                    });
                                    diff.op.BigMapUpdates = (diff.op.BigMapUpdates ?? 0) + 1;
                                    #endregion
                                }
                            }
                            else if (update.Value != null) // WTF: edo2net:34839 - non-existent key was removed
                            {
                                #region add key
                                Db.TryAttach(bigMap);
                                bigMap.LastLevel = diff.op.Level;
                                bigMap.ActiveKeys++;
                                bigMap.TotalKeys++;
                                bigMap.Updates++;

                                key = new BigMapKey
                                {
                                    Id = Cache.AppState.NextBigMapKeyId(),
                                    Active = true,
                                    BigMapPtr = update.Ptr,
                                    FirstLevel = diff.op.Level,
                                    LastLevel = diff.op.Level,
                                    JsonKey = bigMap.Schema.Key.Humanize(update.Key),
                                    JsonValue = bigMap.Schema.Value.Humanize(update.Value),
                                    RawKey = bigMap.Schema.Key.Optimize(update.Key).ToBytes(),
                                    RawValue = bigMap.Schema.Value.Optimize(update.Value).ToBytes(),
                                    KeyHash = update.KeyHash,
                                    Updates = 1
                                };

                                Db.BigMapKeys.Add(key);
                                Cache.BigMapKeys.Cache(key);

                                Db.BigMapUpdates.Add(new BigMapUpdate
                                {
                                    Id = Cache.AppState.NextBigMapUpdateId(),
                                    Action = BigMapAction.AddKey,
                                    BigMapKeyId = key.Id,
                                    BigMapPtr = key.BigMapPtr,
                                    JsonValue = key.JsonValue,
                                    RawValue = key.RawValue,
                                    Level = key.LastLevel,
                                    TransactionId = (diff.op as TransactionOperation)?.Id,
                                    OriginationId = (diff.op as OriginationOperation)?.Id
                                });
                                diff.op.BigMapUpdates = (diff.op.BigMapUpdates ?? 0) + 1;
                                #endregion
                            }
                        }
                        else
                        {
                            #region update temp
                            if (!images.TryGetValue(update.Ptr, out var image))
                                throw new Exception("Can't update non-existent temporary big_map");

                            if (image.TryGetValue(update.KeyHash, out var key))
                            {
                                if (update.Value != null)
                                {
                                    key.RawValue = update.Value.ToBytes();
                                }
                                else
                                {
                                    image.Remove(update.KeyHash);
                                }
                            }
                            else if (update.Value != null) // WTF: edo2net:34839 - non-existent key was removed
                            {
                                image.Add(update.KeyHash, new BigMapKey
                                {
                                    KeyHash = update.KeyHash,
                                    RawKey = update.Key.ToBytes(),
                                    RawValue = update.Value.ToBytes()
                                });
                            }
                            #endregion
                        }
                        break;
                    case RemoveDiff remove:
                        if (remove.Ptr >= 0)
                        {
                            Db.BigMapUpdates.Add(new BigMapUpdate
                            {
                                Id = Cache.AppState.NextBigMapUpdateId(),
                                Action = BigMapAction.Remove,
                                BigMapPtr = remove.Ptr,
                                Level = diff.op.Level,
                                TransactionId = (diff.op as TransactionOperation)?.Id,
                                OriginationId = (diff.op as OriginationOperation)?.Id
                            });
                            diff.op.BigMapUpdates = (diff.op.BigMapUpdates ?? 0) + 1;

                            var removed = Cache.BigMaps.Get(remove.Ptr);
                            Db.TryAttach(removed);
                            removed.Active = false;
                            removed.LastLevel = diff.op.Level;
                            removed.Updates++;
                        }
                        else
                        {
                            // is it possible?
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        int GetOrigin(CopyDiff copy)
        {
            return Diffs
                .FirstOrDefault(x => x.diff.Action == BigMapDiffAction.Copy && x.diff.Ptr == copy.SourcePtr).diff is CopyDiff prevCopy
                    ? GetOrigin(prevCopy)
                    : copy.SourcePtr;
        }

        public virtual async Task Revert(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.Bigmaps))
            {
                var bigmaps = await Db.BigMaps.Where(x => x.LastLevel == block.Level).ToListAsync();
                var keys = await Db.BigMapKeys.Where(x => x.LastLevel == block.Level).ToListAsync();
                var updates = await Db.BigMapUpdates
                    .AsNoTracking()
                    .Where(x => x.Level == block.Level)
                    .Select(x => new
                    {
                        Ptr = x.BigMapPtr,
                        KeyId = x.BigMapKeyId
                    })
                    .ToListAsync();

                await Db.Database.ExecuteSqlRawAsync(@$"
                    DELETE FROM ""BigMapUpdates"" WHERE ""Level"" = {block.Level};
                ");

                foreach (var key in keys)
                {
                    var bigmap = bigmaps.First(x => x.Ptr == key.BigMapPtr);
                    Cache.BigMaps.Cache(bigmap);
                    Cache.BigMapKeys.Cache(key);

                    if (key.FirstLevel == block.Level)
                    {
                        if (key.Active) bigmap.ActiveKeys--;
                        bigmap.TotalKeys--;
                        Db.BigMapKeys.Remove(key);
                        Cache.BigMapKeys.Remove(key);
                    }
                    else
                    {
                        var prevUpdate = await Db.BigMapUpdates
                            .Where(x => x.BigMapKeyId == key.Id)
                            .OrderByDescending(x => x.Id)
                            .FirstAsync();

                        var prevActive = prevUpdate.Action != BigMapAction.RemoveKey;
                        if (key.Active && !prevActive)
                            bigmap.ActiveKeys--;
                        else if (!key.Active && prevActive)
                            bigmap.ActiveKeys++;

                        key.Active = prevActive;
                        key.JsonValue = prevUpdate.JsonValue;
                        key.RawValue = prevUpdate.RawValue;
                        key.LastLevel = prevUpdate.Level;
                        key.Updates -= updates.Count(x => x.KeyId == key.Id);
                    }
                }

                foreach (var bigmap in bigmaps)
                {
                    Cache.BigMaps.Cache(bigmap);
                    if (bigmap.FirstLevel == block.Level)
                    {
                        Db.BigMaps.Remove(bigmap);
                        Cache.BigMaps.Remove(bigmap);
                    }
                    else
                    {
                        bigmap.Active = true;
                        bigmap.Updates -= updates.Count(x => x.Ptr == bigmap.Ptr);
                        bigmap.LastLevel = bigmap.Updates > 1
                            ? (await Db.BigMapUpdates
                                .Where(x => x.BigMapPtr == bigmap.Ptr)
                                .OrderByDescending(x => x.Id)
                                .FirstAsync())
                                .Level
                            : bigmap.FirstLevel;
                    }
                }
            }
        }
    }
}
