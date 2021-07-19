using System.Linq;
using Netezos.Encoding;
using Netezos.Utils;

namespace Tzkt.Sync.Protocols
{
    static class OriginationNonce
    {
        public static string GetContractAddress(int index)
        {
            var hash = new byte[32]
            {
                72, 1, 189, 111, 8, 152, 13, 214, 38, 163, 228, 90, 249, 51, 127, 242,
                206, 198, 138, 55, 219, 134, 18, 158, 128, 96, 185, 73, 180, 9, 86, 229
            };
            var address = Blake2b.GetDigest(hash.Concat(GetBytes(index)).ToArray(), 160);
            return Base58.Convert(address, new byte[] { 2, 90, 121 });
        }

        static byte[] GetBytes(int value) => new byte[4]
        {
            (byte)((value >> 24) & 0xff),
            (byte)((value >> 16) & 0xff),
            (byte)((value >> 8) & 0xff),
            (byte)(value & 0xff),
        };
    }
}
