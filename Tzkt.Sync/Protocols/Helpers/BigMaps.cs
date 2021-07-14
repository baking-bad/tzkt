using Netezos.Contracts;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols
{
    static class BigMaps
    {
        public static BigMapTag GetTags(TreeView bigmap)
        {
            var tags = BigMapTag.None;
            if (bigmap.Name == "token_metadata")
            {
                var schema = bigmap.Schema as BigMapSchema;
                if (IsTopLevel(bigmap) &&
                    schema.Key is NatSchema &&
                    schema.Value is PairSchema pair &&
                        pair.Left is NatSchema nat && nat.Field == "token_id" &&
                        pair.Right is MapSchema map && map.Field == "token_info" &&
                            map.Key is StringSchema &&
                            map.Value is BytesSchema)
                    tags |= BigMapTag.TokenMetadata;
            }
            else if (bigmap.Name == "metadata")
            {
                var schema = bigmap.Schema as BigMapSchema;
                if (IsTopLevel(bigmap) &&
                    schema.Key is StringSchema &&
                    schema.Value is BytesSchema)
                    tags |= BigMapTag.Metadata;
            }
            return tags;
        }

        static bool IsTopLevel(TreeView node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                if (parent.Schema is not PairSchema)
                    return false;
                parent = parent.Parent;
            }
            return true;
        }
    }
}
