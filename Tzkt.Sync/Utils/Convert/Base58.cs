using System;
using System.Numerics;
using System.Security.Cryptography;

namespace Tzkt.Sync.Utils
{
    public static class Base58
    {
        const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        static readonly byte[] Base58Ascii = new byte[]
        {
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 0, 1, 2, 3, 4, 5, 6, 7, 8, 255, 255, 255, 255, 255, 255, 255, 9, 10, 11,
            12, 13, 14, 15, 16, 255, 17, 18, 19, 20, 21, 255, 22, 23, 24, 25, 26, 27, 28,
            29, 30, 31, 32, 255, 255, 255, 255, 255, 255, 33, 34, 35, 36, 37, 38, 39, 40,
            41, 42, 43, 255, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255,
        };

        static byte[] CheckSum(byte[] data)
        {
            using (var shA256 = new SHA256Managed())
            {
                var hash1 = shA256.ComputeHash(data);
                var hash2 = shA256.ComputeHash(hash1);
                return new[] { hash2[0], hash2[1], hash2[2], hash2[3], };
            }
        }

        static byte[] VerifyAndRemoveCheckSum(byte[] bytes)
        {
            var data = bytes.GetBytes(0, bytes.Length - 4);

            var checkSum = CheckSum(data);
            if (bytes[bytes.Length - 4] != checkSum[0] ||
                bytes[bytes.Length - 3] != checkSum[1] ||
                bytes[bytes.Length - 2] != checkSum[2] ||
                bytes[bytes.Length - 1] != checkSum[3])
                throw new FormatException("Checksum is invalid");

            return data;
        }

        static string EncodePlain(byte[] bytes)
        {
            var bigInt = new BigInteger((bytes[0] >= 128
                ? new byte[] { 0 }.Concat(bytes)
                : bytes).Reverse());

            var str = "";
            while (bigInt > 0)
            {
                str = Alphabet[(int)(bigInt % 58)] + str;
                bigInt /= 58;
            }

            var i = 0;
            while (bytes[i++] == 0)
                str = "1" + str;

            return str;
        }

        static byte[] DecodePlain(string base58)
        {
            BigInteger bigInt = 0;
            for (int i = 0; i < base58.Length; ++i)
            {
                var num = Base58Ascii[base58[i]];
                if (num == 255) throw new FormatException($"Invalid Base58 string");
                bigInt = bigInt * 58 + num;
            }

            var cnt = -1;
            while (base58[++cnt] == '1') ;

            var bytes = bigInt.ToByteArray().Reverse();
            return new byte[cnt].Concat(bytes[0] == 0 ? bytes.GetBytes(1, bytes.Length - 1) : bytes);
        }

        public static byte[] Parse(string base58)
        {
            return VerifyAndRemoveCheckSum(DecodePlain(base58));
        }

        public static byte[] Parse(string base58, byte[] prefix)
        {
            var bytes = VerifyAndRemoveCheckSum(DecodePlain(base58));
            return bytes.GetBytes(prefix.Length, bytes.Length - prefix.Length);
        }

        public static byte[] Parse(string base58, int prefixLength)
        {
            var bytes = VerifyAndRemoveCheckSum(DecodePlain(base58));
            return bytes.GetBytes(prefixLength, bytes.Length - prefixLength);
        }

        public static string Convert(byte[] bytes)
        {
            return EncodePlain(bytes.Concat(CheckSum(bytes)));
        }

        public static string Convert(byte[] bytes, byte[] prefix)
        {
            var data = prefix.Concat(bytes);
            return EncodePlain(data.Concat(CheckSum(data)));
        }
    }
}
