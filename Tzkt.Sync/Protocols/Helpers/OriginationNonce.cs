using Netezos.Encoding;
using Netezos.Utils;

namespace Tzkt.Sync.Protocols
{
    static class OriginationNonce
    {
        public static string GetContractAddress(int index)
        {
            byte[] hash =
            [
                72, 1, 189, 111, 8, 152, 13, 214, 38, 163, 228, 90, 249, 51, 127, 242,
                206, 198, 138, 55, 219, 134, 18, 158, 128, 96, 185, 73, 180, 9, 86, 229
            ];
            var address = Blake2b.GetDigest([.. hash, .. GetBytes(index)], 160);
            return Base58.Convert(address, [2, 90, 121]);
        }

        static byte[] GetBytes(int value) =>
        [
            (byte)((value >> 24) & 0xff),
            (byte)((value >> 16) & 0xff),
            (byte)((value >> 8) & 0xff),
            (byte)(value & 0xff),
        ];
    }
}
