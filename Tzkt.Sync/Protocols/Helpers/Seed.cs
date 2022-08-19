using System;
using System.Collections.Generic;
using Blake2Fast;

namespace Tzkt.Sync.Protocols
{
    static class Seed
    {
        public static List<byte[]> GetInitialSeeds(int count, byte[] initialSeed)
        {
            var list = new List<byte[]>(count)
            {
                Blake2b.ComputeHash(32, initialSeed)
            };
            for (int i = 1; i < count; i++)
            {
                list.Add(Blake2b.ComputeHash(32, list[^1].Concat(new byte[32])));
            }
            return list;
        }

        public static byte[] GetNextSeed(byte[] seed, IEnumerable<byte[]> nonces, byte[] vdfSolution)
        {
            var res = Blake2b.ComputeHash(32, seed.Concat(new byte[32]));
            foreach (var nonce in nonces)
                res = Blake2b.ComputeHash(32, res.Concat(nonce));
            if (vdfSolution != null)
                res = Blake2b.ComputeHash(32, res.Concat(vdfSolution));
            return res;
        }

        public static int GetSnapshotIndex(byte[] seed, int snapshots, bool ithaca = false)
        {
            var state = Blake2b.ComputeHash(32, Blake2b.ComputeHash(32, seed.Concat(ithaca
                ? new byte[46]
                {
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    115, 116, 97, 107, 101, 95, 115, 110, 97, 112, 115, 104, 111, 116
                }
                : new byte[45]
                {
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    114, 111, 108, 108, 95, 115, 110, 97, 112, 115, 104, 111, 116
                })));
            var max = int.MaxValue - int.MaxValue % snapshots;
            var tries = 1_000_000;
            while (--tries > 0)
            {
                state = Blake2b.ComputeHash(32, state);
                var r = state.ReadInt32(0);
                r = r == int.MinValue ? 0 : Math.Abs(r);
                if (r < max) return r % snapshots;
            }
            throw new Exception("You are lucky :D");
        }
    }
}
