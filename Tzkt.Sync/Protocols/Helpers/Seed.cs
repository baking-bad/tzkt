using System;
using System.Collections.Generic;
using Blake2Fast;

namespace Tzkt.Sync.Protocols
{
    static class Seed
    {
        const int SnapshotsCount = 16;

        public static List<byte[]> GetInitialSeeds(int count)
        {
            var list = new List<byte[]>(count)
            {
                Blake2b.ComputeHash(32, Array.Empty<byte>())
            };
            for (int i = 1; i < count; i++)
            {
                list.Add(Blake2b.ComputeHash(32, list[^1].Concat(new byte[32])));
            }
            return list;
        }

        public static byte[] GetNextSeed(byte[] seed, IEnumerable<byte[]> nonces)
        {
            var res = Blake2b.ComputeHash(32, seed.Concat(new byte[32]));
            foreach (var nonce in nonces)
            {
                res = Blake2b.ComputeHash(32, res.Concat(nonce));
            }
            return res;
        }

        public static int GetSnapshotIndex(byte[] seed)
        {
            var state = Blake2b.ComputeHash(32, Blake2b.ComputeHash(32, seed.Concat(new byte[45]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                114, 111, 108, 108, 95, 115, 110, 97, 112, 115, 104, 111, 116
            })));
            var max = int.MaxValue - int.MaxValue % SnapshotsCount;
            var tries = 1_000_000;
            while (--tries > 0)
            {
                state = Blake2b.ComputeHash(32, state);
                var r = Math.Abs(state.ReadInt32(0));
                if (r < max) return r % SnapshotsCount;
            }
            throw new Exception("You are lucky :D");
        }
    }
}
