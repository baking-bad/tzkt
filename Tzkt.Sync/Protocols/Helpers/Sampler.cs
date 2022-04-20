using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols
{
    class Sampler
    {
        public int Length => Bakers.Length;

        readonly int[] Alias;
        readonly int[] Bakers;
        readonly long[] P;
        readonly long Total;

        public Sampler(int[] bakers, long[] stakes)
        {
            var cnt = stakes.Length;
            var p = new long[cnt];
            var alias = new int[cnt];
            var small = new List<Item>(cnt);
            var large = new List<Item>(cnt);
            var total = stakes.Sum();

            var ind = 0;
            foreach (var stake in stakes)
            {
                var item = new Item
                {
                    Index = ind++,
                    P = stake,
                    Q = stake * cnt
                };
                if (item.Q < total) small.Add(item);
                else large.Add(item);
            }

            while (true)
            {
                if (small.Count > 0 && large.Count > 0)
                {
                    var l = small[^1];
                    var g = large[^1];

                    small.RemoveAt(small.Count - 1);

                    p[l.Index] = l.Q;
                    alias[l.Index] = g.Index;

                    g.Q += l.Q - total;
                    if (g.Q < total)
                    {
                        large.RemoveAt(large.Count - 1);
                        small.Add(g);
                    }
                }
                else if (small.Count > 0)
                {
                    foreach (var l in small)
                    {
                        p[l.Index] = total;
                        alias[l.Index] = -1;
                    }
                    break;
                }
                else if (large.Count > 0)
                {
                    foreach (var g in large)
                    {
                        p[g.Index] = total;
                        alias[g.Index] = -1;
                    }
                    break;
                }
                else
                {
                    throw new Exception("Sampler initialization failed");
                }
            }

            (P, Alias, Total) = (p, alias, total);
            Bakers = bakers;
        }

        public int GetBaker(byte[] seed)
        {
            var state = Blake2Fast.Blake2b.ComputeHash(32, seed);
            var i = TakeInt64(state, 0, P.Length, out var nextState, out var nextPos);
            var el = TakeInt64(nextState, nextPos, Total, out var _, out var _);
            return el < P[i] ? Bakers[i] : Bakers[Alias[i]];
        }

        public static async Task<Sampler> CreateAsync(ProtocolHandler proto, int cycle)
        {
            var block = proto.Cache.AppState.GetLevel();
            var result = (await proto.Rpc.GetStakeDistribution(block, cycle))
                .EnumerateArray()
                .Where(x => x.RequiredInt64("active_stake") > 0);

            return new Sampler
            (
                result.Select(x => proto.Cache.Accounts.GetDelegate(x.RequiredString("baker")).Id).ToArray(),
                result.Select(x => x.RequiredInt64("active_stake")).ToArray()
            );
        }

        static long TakeInt64(byte[] state, int pos, long bound, out byte[] nextState, out int nextPos)
        {
            if (pos > state.Length - 8)
                return TakeInt64(Blake2Fast.Blake2b.ComputeHash(32, state), 0, bound, out nextState, out nextPos);

            var r = Math.Abs(state.ReadInt64(pos));
            if (r >= long.MaxValue - long.MaxValue % bound)
                return TakeInt64(state, pos + 8, bound, out nextState, out nextPos);

            nextState = state;
            nextPos = pos + 8;
            return r % bound;
        }

        class Item
        {
            public int Index { get; set; }
            public long Q { get; set; }
            public long P { get; set; }
        }
    }
}
