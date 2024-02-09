﻿using Netmavryk.Contracts;
using Netmavryk.Encoding;

namespace Mvkt.Sync
{
    static class NetmavrykExtension
    {
        static readonly byte[] mv1 = new byte[] { 5, 186, 196 };
        static readonly byte[] mv2 = new byte[] { 5, 186, 199 };
        static readonly byte[] mv3 = new byte[] { 5, 186, 201 };
        static readonly byte[] mv4 = new byte[] { 5, 186, 204 };
        static readonly byte[] KT1 = new byte[] { 2, 90, 121 };
        static readonly byte[] txr1 = new byte[] { 1, 128, 120, 31 };
        static readonly byte[] sr1 = new byte[] { 6, 124, 117 };

        public static string ParseAddress(this IMicheline micheline)
        {
            if (micheline is MichelineString s)
            {
                if (s.Value.Length == 36)
                    return s.Value;

                if (s.Value.StartsWith("txr1"))
                    return s.Value.Length == 37 ? s.Value : s.Value[..37];

                return s.Value[..36];
            }

            var value = (micheline as MichelineBytes).Value;
            byte[] prefix;
            byte[] bytes;
            if (value[0] == 0)
            {
                prefix = value[1] switch
                {
                    0 => mv1,
                    1 => mv2,
                    2 => mv3,
                    _ => mv4
                };
                bytes = value.GetBytes(2, 20);
            }
            else
            {
                prefix = value[0] switch
                {
                    1 => KT1,
                    2 => txr1,
                    _ => sr1,
                };
                bytes = value.GetBytes(1, 20);
            }
            return Base58.Convert(bytes, prefix);
        }

        public static bool TryParseAddress(this IMicheline micheline, out string res)
        {
            if (micheline is MichelineString s && s.Value.Length >= 36)
            {
                if (s.Value.Length == 36)
                    res = s.Value;
                else if (s.Value.StartsWith("txr1"))
                    res = s.Value.Length == 37 ? s.Value : s.Value[..37];
                else
                    res = s.Value[..36];

                return true;
            }

            if (micheline is MichelineBytes micheBytes && micheBytes.Value.Length >= 22)
            {
                var value = micheBytes.Value;
                if (value[0] == 0)
                {
                    if (value[1] == 0)
                    {
                        res = Base58.Convert(value.GetBytes(2, 20), mv1);
                        return true;
                    }
                    else if (value[1] == 1)
                    {
                        res = Base58.Convert(value.GetBytes(2, 20), mv2);
                        return true;
                    }
                    else if (value[1] == 2)
                    {
                        res = Base58.Convert(value.GetBytes(2, 20), mv3);
                        return true;
                    }
                    else if (value[1] == 3)
                    {
                        res = Base58.Convert(value.GetBytes(2, 20), mv4);
                        return true;
                    }
                }
                else if (value[0] == 1 && value[21] == 0)
                {
                    res = Base58.Convert(value.GetBytes(1, 20), KT1);
                    return true;
                }
                else if (value[0] == 2 && value[21] == 0)
                {
                    res = Base58.Convert(value.GetBytes(1, 20), txr1);
                    return true;
                }
                else if (value[0] == 3 && value[21] == 0)
                {
                    res = Base58.Convert(value.GetBytes(1, 20), sr1);
                    return true;
                }
            }

            res = null;
            return false;
        }

        public static IMicheline Replace(this IMicheline micheline, IMicheline oldNode, IMicheline newNode)
        {
            if (micheline == oldNode)
                return newNode;

            if (micheline is MichelineArray arr && arr.Count > 0)
            {
                for (int i = 0; i < arr.Count; i++)
                    arr[i] = arr[i].Replace(oldNode, newNode);
                return arr;
            }

            if (micheline is MichelinePrim prim && prim.Args != null)
            {
                for (int i = 0; i < prim.Args.Count; i++)
                    prim.Args[i] = prim.Args[i].Replace(oldNode, newNode);
                return prim;
            }

            return micheline;
        }

        public static IEnumerable<Schema> Children(this PairSchema pair)
        {
            if (pair.Left is PairSchema left && left.Name == null)
            {
                foreach (var child in left.Children())
                    yield return child;
            }
            else
            {
                yield return pair.Left;
            }

            if (pair.Right is PairSchema right && right.Name == null)
            {
                foreach (var child in right.Children())
                    yield return child;
            }
            else
            {
                yield return pair.Right;
            }
        }
    }
}
