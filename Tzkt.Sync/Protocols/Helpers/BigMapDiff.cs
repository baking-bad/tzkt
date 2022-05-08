using System.Collections.Generic;
using System.Text.Json;
using Netezos.Encoding;

namespace Tzkt.Sync.Protocols
{
    public class AllocDiff : BigMapDiff
    {
        public override BigMapDiffAction Action => BigMapDiffAction.Alloc;
    }

    public class CopyDiff : BigMapDiff
    {
        public override BigMapDiffAction Action => BigMapDiffAction.Copy;
        public int SourcePtr { get; set; }
    }

    public class UpdateDiff : BigMapDiff
    {
        public override BigMapDiffAction Action => BigMapDiffAction.Update;

        public string KeyHash { get; set; }
        public IMicheline Key { get; set; }
        public IMicheline Value { get; set; }
    }

    public class RemoveDiff : BigMapDiff
    {
        public override BigMapDiffAction Action => BigMapDiffAction.Remove;
    }

    public abstract class BigMapDiff
    {
        #region static
        public static BigMapDiff Parse(JsonElement diff)
        {
            return diff.RequiredString("action") switch
            {
                "alloc" => new AllocDiff
                {
                    Ptr = diff.RequiredInt32("big_map")
                },
                "copy" => new CopyDiff
                {
                    Ptr = diff.RequiredInt32("destination_big_map"),
                    SourcePtr = diff.RequiredInt32("source_big_map")
                },
                "update" => new UpdateDiff
                {
                    Ptr = diff.RequiredInt32("big_map"),
                    KeyHash = diff.RequiredString("key_hash"),
                    Key = Micheline.FromJson(diff.Required("key")),
                    Value = diff.TryGetProperty("value", out var v)
                        ? Micheline.FromJson(v)
                        : null
                },
                "remove" => new RemoveDiff
                {
                    Ptr = diff.RequiredInt32("big_map")
                },
                _ => throw new ValidationException($"Unknown big_map_diff action")
            };
        }

        public static IEnumerable<BigMapDiff> ParseLazyStorage(JsonElement diffs)
        {
            foreach (var diff in diffs.RequiredArray().EnumerateArray())
            {
                if (diff.RequiredString("kind") == "big_map")
                {
                    var content = diff.Required("diff");
                    switch (content.RequiredString("action"))
                    {
                        case "update":
                            foreach (var update in content.RequiredArray("updates").EnumerateArray())
                            {
                                yield return new UpdateDiff
                                {
                                    Ptr = diff.RequiredInt32("id"),
                                    KeyHash = update.RequiredString("key_hash"),
                                    Key = Micheline.FromJson(update.Required("key")),
                                    Value = update.TryGetProperty("value", out var v)
                                        ? Micheline.FromJson(v)
                                        : null
                                };
                            }
                            break;
                        case "remove":
                            yield return new RemoveDiff
                            {
                                Ptr = diff.RequiredInt32("id")
                            };
                            break;
                        case "copy":
                            yield return new CopyDiff
                            {
                                Ptr = diff.RequiredInt32("id"),
                                SourcePtr = content.RequiredInt32("source")
                            };
                            foreach (var update in content.RequiredArray("updates").EnumerateArray())
                            {
                                yield return new UpdateDiff
                                {
                                    Ptr = diff.RequiredInt32("id"),
                                    KeyHash = update.RequiredString("key_hash"),
                                    Key = Micheline.FromJson(update.Required("key")),
                                    Value = update.TryGetProperty("value", out var v)
                                        ? Micheline.FromJson(v)
                                        : null
                                };
                            }
                            break;
                        case "alloc":
                            yield return new AllocDiff
                            {
                                Ptr = diff.RequiredInt32("id")
                            };
                            foreach (var update in content.RequiredArray("updates").EnumerateArray())
                            {
                                yield return new UpdateDiff
                                {
                                    Ptr = diff.RequiredInt32("id"),
                                    KeyHash = update.RequiredString("key_hash"),
                                    Key = Micheline.FromJson(update.Required("key")),
                                    Value = update.TryGetProperty("value", out var v)
                                        ? Micheline.FromJson(v)
                                        : null
                                };
                            }
                            break;
                        default:
                            throw new ValidationException("Unknown bigmap diff action");
                    }
                }
            }
        }
        #endregion

        public abstract BigMapDiffAction Action { get; }
        public int Ptr { get; set; }
    }

    public enum BigMapDiffAction
    {
        Alloc,
        Copy,
        Update,
        Remove
    }
}
