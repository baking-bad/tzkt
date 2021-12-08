using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols
{
    static class Constants
    {
        public static async Task<List<RegisterConstantOperation>> Find(TzktContext db, IEnumerable<IMicheline> nodes)
        {
            var res = new List<RegisterConstantOperation>();
            while (true)
            {
                var pendingConstants = new HashSet<string>(Find(nodes));
                foreach (var constant in res)
                    pendingConstants.Remove(constant.Address);

                if (pendingConstants.Count == 0)
                    break;

                var constants = await db.RegisterConstantOps
                    .Where(x => pendingConstants.Contains(x.Address))
                    .ToListAsync();

                foreach (var constant in constants)
                    res.Add(constant);

                nodes = constants.Select(x => Micheline.FromBytes(x.Value));
            }
            return res;
        }

        public static IEnumerable<string> Find(IEnumerable<IMicheline> nodes)
        {
            foreach (var node in nodes)
                foreach (var c in Find(node))
                    yield return c;
        }

        // max recursion depth 10000, according to Michelson docs
        public static IEnumerable<string> Find(IMicheline node)
        {
            if (node is MichelineArray array)
            {
                foreach (var item in array)
                    foreach (var c in Find(item))
                        yield return c;
            }
            else if (node is MichelinePrim prim && prim.Args != null)
            {
                if (prim.Prim == PrimType.constant)
                {
                    yield return (prim.Args[0] as MichelineString).Value;
                }
                else
                {
                    foreach (var arg in prim.Args)
                        foreach (var c in Find(arg))
                            yield return c;
                }
            }
        }

        // max recursion depth 10000, according to Michelson docs
        public static IMicheline Expand(IMicheline node, Dictionary<string, IMicheline> constants)
        {
            if (node is MichelineArray array)
            {
                for (int i = 0; i < array.Count; i++)
                    array[i] = Expand(array[i], constants);
                return array;
            }

            if (node is not MichelinePrim prim || prim.Args == null)
                return node;

            if (prim.Prim == PrimType.constant)
                return Expand(constants[(prim.Args[0] as MichelineString).Value], constants);

            for (int i = 0; i < prim.Args.Count; i++)
                prim.Args[i] = Expand(prim.Args[i], constants);

            return prim;
        }
    }
}
