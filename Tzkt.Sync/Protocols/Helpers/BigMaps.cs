using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Netezos.Contracts;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols
{
    static class BigMaps
    {
        public static BigMapTag GetTags(Contract contract, TreeView bigmap)
        {
            var tags = BigMapTag.None;
            if (IsPersistent(bigmap))
            {
                tags |= BigMapTag.Persistent;
                var schema = bigmap.Schema as BigMapSchema;
                if (schema.Field == "metadata")
                {
                    if (schema.Key is StringSchema &&
                        schema.Value is BytesSchema)
                        tags |= BigMapTag.Metadata;
                }
                else if (contract.Kind == ContractKind.Asset)
                {
                    if (schema.Field == "token_metadata")
                    {
                        if (schema.Key is NatSchema &&
                            schema.Value is PairSchema pair &&
                                pair.Left is NatSchema &&
                                pair.Right is MapSchema map &&
                                    map.Key is StringSchema &&
                                    map.Value is BytesSchema)
                            tags |= BigMapTag.TokenMetadata;
                    }
                    else if (schema.Field == "ledger")
                    {
                        tags |= GetLedgerType(schema);
                    }
                }
            }
            return tags;
        }

        public static BigMapTag GetLedgerType(BigMapSchema schema)
        {
            switch ((schema.Key, schema.Value))
            {
                case (AddressSchema, NatSchema):
                    return BigMapTag.Ledger1;

                case (NatSchema, AddressSchema):
                    return BigMapTag.Ledger2;

                case (PairSchema p1, NatSchema):
                    if (p1.Left is AddressSchema && p1.Right is NatSchema)
                        return BigMapTag.Ledger3;
                    if (p1.Left is NatSchema && p1.Right is AddressSchema)
                        return BigMapTag.Ledger4;
                    break;

                case (AddressSchema, PairSchema p2):
                    if (p2.Left is NatSchema &&
                        p2.Right is MapSchema mapR &&
                        mapR.Key is AddressSchema &&
                        mapR.Value is NatSchema)
                        return BigMapTag.Ledger5;
                    if (p2.Left is MapSchema mapL &&
                        mapL.Key is AddressSchema &&
                        mapL.Value is NatSchema &&
                        p2.Right is NatSchema)
                        return BigMapTag.Ledger6;
                    if (p2.Children().Any(x => x is NatSchema && x.Name == "balance"))
                        return BigMapTag.Ledger8;
                    break;

                case (PairSchema p3, PairSchema p4):
                    if (p3.Left is AddressSchema && p3.Right is NatSchema &&
                        p4.Children().Any(x => x is NatSchema && x.Name == "balance"))
                        return BigMapTag.Ledger9;
                    if (p3.Left is NatSchema && p3.Right is AddressSchema &&
                        p4.Children().Any(x => x is NatSchema && x.Name == "balance"))
                        return BigMapTag.Ledger10;
                    break;

                default:
                    break;
            }
            return BigMapTag.None;
        }

        public static List<(string Address, BigInteger TokeId, BigInteger Balance)> ParseLedger(BigMap bigmap, BigMapKey key, BigMapUpdate update)
        {
            switch (bigmap.Tags & BigMapTag.LedgerMask)
            {
                case BigMapTag.Ledger1:
                    return new(1)
                    {
                        (
                            key.JsonKey[1..37],
                            BigInteger.Zero,
                            update.Action != BigMapAction.RemoveKey
                                ? (Micheline.FromBytes(update.RawValue) as MichelineInt).Value
                                : BigInteger.Zero
                        )
                    };
                case BigMapTag.Ledger2:
                    return new(1)
                    {
                        (
                            update.JsonValue[1..37],
                            (Micheline.FromBytes(key.RawKey) as MichelineInt).Value,
                            update.Action != BigMapAction.RemoveKey
                                ? BigInteger.One
                                : BigInteger.Zero
                        )
                    };
                case BigMapTag.Ledger3:
                    var pair = Micheline.FromBytes(key.RawKey) as MichelinePrim;
                    return new(1)
                    {
                        (
                            pair.Args[0].ParseAddress(),
                            (pair.Args[1] as MichelineInt).Value,
                            update.Action != BigMapAction.RemoveKey
                                ? (Micheline.FromBytes(update.RawValue) as MichelineInt).Value
                                : BigInteger.Zero
                        )
                    };
                case BigMapTag.Ledger4:
                    pair = Micheline.FromBytes(key.RawKey) as MichelinePrim;
                    return new(1)
                    {
                        (
                            pair.Args[1].ParseAddress(),
                            (pair.Args[0] as MichelineInt).Value,
                            update.Action != BigMapAction.RemoveKey
                                ? (Micheline.FromBytes(update.RawValue) as MichelineInt).Value
                                : BigInteger.Zero
                        )
                    };
                case BigMapTag.Ledger5:
                    pair = Micheline.FromBytes(update.RawValue) as MichelinePrim;
                    return new(1)
                    {
                        (
                            key.JsonKey[1..37],
                            BigInteger.Zero,
                            update.Action != BigMapAction.RemoveKey
                                ? (pair.Args[0] as MichelineInt).Value
                                : BigInteger.Zero
                        )
                    };
                case BigMapTag.Ledger6:
                    pair = Micheline.FromBytes(update.RawValue) as MichelinePrim;
                    return new(1)
                    {
                        (
                            key.JsonKey[1..37],
                            BigInteger.Zero,
                            update.Action != BigMapAction.RemoveKey
                                ? (pair.Args[1] as MichelineInt).Value
                                : BigInteger.Zero
                        )
                    };
                case BigMapTag.Ledger7: // custom handler for tzBTC
                    var micheKey = Micheline.Unpack((Micheline.FromBytes(key.RawKey) as MichelineBytes).Value);
                    if (micheKey is MichelinePrim keyPrim && keyPrim.Args?.Count == 2 &&
                        keyPrim.Args[0] is MichelineString keyType && keyType.Value == "ledger" &&
                        keyPrim.Args[1].TryParseAddress(out var address))
                    {
                        var micheValue = Micheline.Unpack((Micheline.FromBytes(update.RawValue) as MichelineBytes).Value);
                        if (micheValue is MichelinePrim valuePrim && valuePrim.Args?.Count == 2 &&
                            valuePrim.Args[0] is MichelineInt balance)
                        {
                            return new(1)
                            {
                                (
                                    address,
                                    BigInteger.Zero,
                                    update.Action != BigMapAction.RemoveKey
                                        ? balance.Value
                                        : BigInteger.Zero
                                )
                            };
                        }
                    }
                    return new(0);
                case BigMapTag.Ledger8:
                    using (var doc = JsonDocument.Parse(update.JsonValue))
                    {
                        return new(1)
                        {
                            (
                                key.JsonKey[1..37],
                                BigInteger.Zero,
                                update.Action != BigMapAction.RemoveKey
                                    ? BigInteger.Parse(doc.RootElement.GetProperty("balance").GetString())
                                    : BigInteger.Zero
                            )
                        };
                    }
                case BigMapTag.Ledger9:
                    pair = Micheline.FromBytes(key.RawKey) as MichelinePrim;
                    using (var doc = JsonDocument.Parse(update.JsonValue))
                    {
                        return new(1)
                        {
                            (
                                pair.Args[0].ParseAddress(),
                                (pair.Args[1] as MichelineInt).Value,
                                update.Action != BigMapAction.RemoveKey
                                    ? BigInteger.Parse(doc.RootElement.GetProperty("balance").GetString())
                                    : BigInteger.Zero
                            )
                        };
                    }
                case BigMapTag.Ledger10:
                    pair = Micheline.FromBytes(key.RawKey) as MichelinePrim;
                    using (var doc = JsonDocument.Parse(update.JsonValue))
                    {
                        return new(1)
                        {
                            (
                                pair.Args[1].ParseAddress(),
                                (pair.Args[0] as MichelineInt).Value,
                                update.Action != BigMapAction.RemoveKey
                                    ? BigInteger.Parse(doc.RootElement.GetProperty("balance").GetString())
                                    : BigInteger.Zero
                            )
                        };
                    }
                case BigMapTag.Ledger11:
                    pair = Micheline.FromBytes(update.RawValue) as MichelinePrim;
                    var balances = pair.Args[0] as MichelineArray;
                    var res = new List<(string, BigInteger, BigInteger)>(balances.Count);
                    foreach (var balance in balances)
                    {
                        var elt = balance as MichelinePrim;
                        res.Add((
                            key.JsonKey[1..37],
                            (elt.Args[0] as MichelineInt).Value,
                            update.Action != BigMapAction.RemoveKey
                                ? (elt.Args[1] as MichelineInt).Value
                                : BigInteger.Zero
                        ));
                    }
                    return res;
                case BigMapTag.Ledger12:
                    pair = (Micheline.FromBytes(update.RawValue) as MichelinePrim).Args[1] as MichelinePrim;
                    var option = pair.Args[1] as MichelinePrim;
                    if (option.Prim == PrimType.Some)
                    {
                        return new(1)
                        {
                            (
                                (pair.Args[0] as MichelinePrim).Args[1].ParseAddress(),
                                (option.Args[0] as MichelineInt).Value,
                                update.Action != BigMapAction.RemoveKey
                                    ? BigInteger.One
                                    : BigInteger.Zero
                            )
                        };
                    }
                    return new(0);
                default:
                    throw new NotSupportedException("Unsupported ledger type");
            }
        }

        static bool IsPersistent(TreeView node)
        {
            while (node.Parent?.Schema is PairSchema)
                node = node.Parent;
            return node.Parent == null;
        }
    }
}
