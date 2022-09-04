using System;
using System.Collections.Generic;
using Netezos.Encoding;

namespace Tzkt.Api
{
    public static class MichelineExt
    {
        public static IEnumerable<MichelinePrim> FindPrimNodes(this IMicheline root, Func<MichelinePrim, bool> predicate)
        {
            var stack = new Stack<IMicheline>();
            stack.Push(root);

            while (stack.Count != 0)
            {
                switch (stack.Pop())
                {
                    case null: break;
                    case MichelinePrim node:
                    {
                        if (predicate(node))
                            yield return node;

                        if (node.Args != null)
                        { 
                            foreach (var arg in node.Args)
                                stack.Push(arg);
                        }
                        break;
                    }
                    case MichelineArray arr:
                    {
                        foreach (var elt in arr)
                            stack.Push(elt);

                        break;
                    }
                }
            }
        }
    }
}