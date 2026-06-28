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
            var schema = (bigmap.Schema as BigMapSchema)!;
            var tags = BigMapTag.None;

            if (IsPersistent(bigmap))
            {
                tags |= BigMapTag.Persistent;
                if (schema.Field == "metadata")
                {
                    if (schema.Key is StringSchema &&
                        schema.Value is BytesSchema)
                        tags |= BigMapTag.Metadata;
                }
                else if (contract.Kind == ContractKind.Asset && schema.Field == "token_metadata")
                {
                    if (schema.Key is NatSchema &&
                        schema.Value is PairSchema pair &&
                            pair.Left is NatSchema &&
                            pair.Right is MapSchema map &&
                                map.Key is StringSchema &&
                                map.Value is BytesSchema)
                        tags |= BigMapTag.TokenMetadata;
                }
            }

            if (contract.Kind == ContractKind.Asset && schema.Field == "ledger")
            {
                tags |= GetLedgerType(schema);
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

        public static List<(string Address, byte[]? Entrypoint, BigInteger TokeId, BigInteger Balance)> ParseLedger(BigMap bigmap, BigMapKey key, BigMapUpdate update)
        {
            var rawKey = Micheline.FromBytes(key.RawKey);
            var rawValue = Micheline.FromBytes(update.RawValue!);
            switch (bigmap.Tags & BigMapTag.LedgerMask)
            {
                case BigMapTag.Ledger1:
                    var (parsedAddress, parsedEntrypoint) = rawKey.ParseAddressWithEntrypoint();
                    return
                    [
                        (
                            parsedAddress,
                            parsedEntrypoint,
                            BigInteger.Zero,
                            update.Action != BigMapAction.RemoveKey
                                ? (rawValue as MichelineInt)!.Value
                                : BigInteger.Zero
                        )
                    ];
                case BigMapTag.Ledger2:
                    (parsedAddress, parsedEntrypoint) = rawValue.ParseAddressWithEntrypoint();
                    return
                    [
                        (
                            parsedAddress,
                            parsedEntrypoint,
                            (rawKey as MichelineInt)!.Value,
                            update.Action != BigMapAction.RemoveKey
                                ? BigInteger.One
                                : BigInteger.Zero
                        )
                    ];
                case BigMapTag.Ledger3:
                    var pair = (rawKey as MichelinePrim)!;
                    (parsedAddress, parsedEntrypoint) = pair.Args![0].ParseAddressWithEntrypoint();
                    return
                    [
                        (
                            parsedAddress,
                            parsedEntrypoint,
                            (pair.Args[1] as MichelineInt)!.Value,
                            update.Action != BigMapAction.RemoveKey
                                ? (rawValue as MichelineInt)!.Value
                                : BigInteger.Zero
                        )
                    ];
                case BigMapTag.Ledger4:
                    pair = (rawKey as MichelinePrim)!;
                    (parsedAddress, parsedEntrypoint) = pair.Args![1].ParseAddressWithEntrypoint();
                    return
                    [
                        (
                            parsedAddress,
                            parsedEntrypoint,
                            (pair.Args[0] as MichelineInt)!.Value,
                            update.Action != BigMapAction.RemoveKey
                                ? (rawValue as MichelineInt)!.Value
                                : BigInteger.Zero
                        )
                    ];
                case BigMapTag.Ledger5:
                    (parsedAddress, parsedEntrypoint) = rawKey.ParseAddressWithEntrypoint();
                    pair = (rawValue as MichelinePrim)!;
                    return
                    [
                        (
                            parsedAddress,
                            parsedEntrypoint,
                            BigInteger.Zero,
                            update.Action != BigMapAction.RemoveKey
                                ? (pair.Args![0] as MichelineInt)!.Value
                                : BigInteger.Zero
                        )
                    ];
                case BigMapTag.Ledger6:
                    (parsedAddress, parsedEntrypoint) = rawKey.ParseAddressWithEntrypoint();
                    pair = (rawValue as MichelinePrim)!;
                    return
                    [
                        (
                            parsedAddress,
                            parsedEntrypoint,
                            BigInteger.Zero,
                            update.Action != BigMapAction.RemoveKey
                                ? (pair.Args![1] as MichelineInt)!.Value
                                : BigInteger.Zero
                        )
                    ];
                case BigMapTag.Ledger7: // custom handler for tzBTC
                    var micheKey = Micheline.Unpack((rawKey as MichelineBytes)!.Value);
                    if (micheKey is MichelinePrim keyPrim && keyPrim.Args?.Count == 2 &&
                        keyPrim.Args[0] is MichelineString keyType && keyType.Value == "ledger" &&
                        keyPrim.Args[1].TryParseAddressWithEntrypoint(out parsedAddress, out parsedEntrypoint))
                    {
                        var micheValue = Micheline.Unpack((rawValue as MichelineBytes)!.Value);
                        if (micheValue is MichelinePrim valuePrim && valuePrim.Args?.Count == 2 &&
                            valuePrim.Args[0] is MichelineInt balance)
                        {
                            return
                            [
                                (
                                    parsedAddress,
                                    parsedEntrypoint,
                                    BigInteger.Zero,
                                    update.Action != BigMapAction.RemoveKey
                                        ? balance.Value
                                        : BigInteger.Zero
                                )
                            ];
                        }
                    }
                    return [];
                case BigMapTag.Ledger8:
                    (parsedAddress, parsedEntrypoint) = rawKey.ParseAddressWithEntrypoint();
                    using (var doc = JsonDocument.Parse(update.JsonValue!))
                    {
                        return
                        [
                            (
                                parsedAddress,
                                parsedEntrypoint,
                                BigInteger.Zero,
                                update.Action != BigMapAction.RemoveKey
                                    ? BigInteger.Parse(doc.RootElement.RequiredString("balance"))
                                    : BigInteger.Zero
                            )
                        ];
                    }
                case BigMapTag.Ledger9:
                    pair = (rawKey as MichelinePrim)!;
                    (parsedAddress, parsedEntrypoint) = pair.Args![0].ParseAddressWithEntrypoint();
                    using (var doc = JsonDocument.Parse(update.JsonValue!))
                    {
                        return
                        [
                            (
                                parsedAddress,
                                parsedEntrypoint,
                                (pair.Args[1] as MichelineInt)!.Value,
                                update.Action != BigMapAction.RemoveKey
                                    ? BigInteger.Parse(doc.RootElement.RequiredString("balance"))
                                    : BigInteger.Zero
                            )
                        ];
                    }
                case BigMapTag.Ledger10:
                    pair = (rawKey as MichelinePrim)!;
                    (parsedAddress, parsedEntrypoint) = pair.Args![1].ParseAddressWithEntrypoint();
                    using (var doc = JsonDocument.Parse(update.JsonValue!))
                    {
                        return
                        [
                            (
                                parsedAddress,
                                parsedEntrypoint,
                                (pair.Args[0] as MichelineInt)!.Value,
                                update.Action != BigMapAction.RemoveKey
                                    ? BigInteger.Parse(doc.RootElement.RequiredString("balance"))
                                    : BigInteger.Zero
                            )
                        ];
                    }
                case BigMapTag.Ledger11:
                    (parsedAddress, parsedEntrypoint) = rawKey.ParseAddressWithEntrypoint();
                    pair = (rawValue as MichelinePrim)!;
                    var balances = (pair.Args![0] as MichelineArray)!;
                    var res = new List<(string, byte[]?, BigInteger, BigInteger)>(balances.Count);
                    foreach (var balance in balances)
                    {
                        var elt = (balance as MichelinePrim)!;
                        res.Add((
                            parsedAddress,
                            parsedEntrypoint,
                            (elt.Args![0] as MichelineInt)!.Value,
                            update.Action != BigMapAction.RemoveKey
                                ? (elt.Args[1] as MichelineInt)!.Value
                                : BigInteger.Zero
                        ));
                    }
                    return res;
                case BigMapTag.Ledger12:
                    pair = ((rawValue as MichelinePrim)!.Args![1] as MichelinePrim)!;
                    var option = (pair.Args![1] as MichelinePrim)!;
                    if (option.Prim == PrimType.Some)
                    {
                        (parsedAddress, parsedEntrypoint) = (pair.Args[0] as MichelinePrim)!.Args![1].ParseAddressWithEntrypoint();
                        return
                        [
                            (
                                parsedAddress,
                                parsedEntrypoint,
                                (option.Args![0] as MichelineInt)!.Value,
                                update.Action != BigMapAction.RemoveKey
                                    ? BigInteger.One
                                    : BigInteger.Zero
                            )
                        ];
                    }
                    return [];
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
