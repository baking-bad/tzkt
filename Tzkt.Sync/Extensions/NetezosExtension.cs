using Netezos.Encoding;

namespace Tzkt.Sync
{
    static class NetezosExtension
    {
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
    }
}
